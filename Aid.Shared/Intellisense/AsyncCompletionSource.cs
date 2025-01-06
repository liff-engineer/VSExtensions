using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using System.Linq;

using static Microsoft.VisualStudio.Threading.AsyncReaderWriterLock;
using Newtonsoft.Json.Linq;

namespace Aid.Shared.Intellisense
{
    class AsyncCompletionSource : IAsyncCompletionSource
    {
        private Requester Requester { get; }
        private ITextStructureNavigatorSelectorService StructureNavigatorSelector { get; }
        private Dictionary<int, ImageElement> ImageElements;

        public AsyncCompletionSource(ITextStructureNavigatorSelectorService structureNavigatorSelector)
        {
            Requester = Requester.Instance;
            StructureNavigatorSelector = structureNavigatorSelector;
            //ThreadHelper.ThrowIfNotOnUIThread();
            //图标缓存
            ImageElements = new();
        }

        public async Task<CompletionContext> GetCompletionContextAsync(IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
        {
            CompletionDescriptor descriptor = Requester.CompletionDescriptor;
            if (descriptor == null)
            {
                return null;
            }

            SnapshotSpan tokenSpan = FindTokenSpanAtPosition(triggerLocation);
            CompletionRequestDescriptor requestDescriptor = new CompletionRequestDescriptor();
            requestDescriptor.ContentType = triggerLocation.Snapshot.ContentType.TypeName;
            requestDescriptor.Word = tokenSpan.GetText();
            requestDescriptor.Position = triggerLocation.Position;
            bool firstSpace = (trigger.Character == ' ');
            if (descriptor.ResponseFirstSpace && firstSpace)
            {
                var spans = GetWords(triggerLocation);
                requestDescriptor.Words = new List<string>();
                foreach (var span in spans)
                {
                    requestDescriptor.Words.Add(span.GetText());
                }
                //if (requestDescriptor.Words.Count > 0)
                //{
                //    requestDescriptor.Word = requestDescriptor.Words[requestDescriptor.Words.Count - 1];
                //}
            }
            else
            {
                requestDescriptor.Content = triggerLocation.Snapshot.GetText();
            }

            List<CompletionSuggestedItem> suggestedItems = await Requester.ExecuteAsync<CompletionSuggestedItem>("CompletionSuggestedItems", null, JObject.FromObject(requestDescriptor), token);
            suggestedItems.RemoveAll(obj => string.IsNullOrEmpty(obj.DisplayText));

            var itemsBuilder = ImmutableArray.CreateBuilder<CompletionItem>();
            foreach (var item in suggestedItems)
            {
                CompletionItem completionItem = new CompletionItem(
                    displayText: item.DisplayText,
                    source: this,
#if VISUALSTUDIOTOOLKIT17
                    icon: item.ImageMonikerId >= 0 ? FindImageElementByImageMonikerId(item.ImageMonikerId) : ImageElement.Empty,
#else
                    icon: FindImageElementByImageMonikerId(item.ImageMonikerId>=0? item.ImageMonikerId:KnownMonikers.IntellisenseLightBulb.Id),
#endif
                    filters: ImmutableArray<CompletionFilter>.Empty,
                    suffix: string.IsNullOrEmpty(item.Suffix) ? string.Empty : item.Suffix,
                    insertText: string.IsNullOrEmpty(item.InsertText) ? item.DisplayText : item.InsertText,
                    sortText: string.IsNullOrEmpty(item.SortText) ? item.DisplayText : item.SortText,
                    filterText: string.IsNullOrEmpty(item.FilterText) ? item.DisplayText : item.FilterText,
#if VISUALSTUDIOTOOLKIT17
                    automationText: string.Empty,
#endif
                    attributeIcons: ImmutableArray<ImageElement>.Empty
                    );
                completionItem.Properties.AddProperty(nameof(CompletionSuggestedItem), item);
                itemsBuilder.Add(completionItem);
            }
            return new CompletionContext(itemsBuilder.ToImmutableArray());
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            if (item.Properties.TryGetProperty<CompletionSuggestedItem>(nameof(CompletionSuggestedItem), out var matchingItem))
            {
                if (string.IsNullOrEmpty(matchingItem.Description))
                {
                    return Task.FromResult<object>(null);
                }
                return Task.FromResult<object>($"{matchingItem.Description}");
            }
            return Task.FromResult<object>(null);
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            //以下不触发
            if (char.IsNumber(trigger.Character) ||
                char.IsPunctuation(trigger.Character) ||
                trigger.Character == '\n' ||
                trigger.Reason == CompletionTriggerReason.Backspace ||
                trigger.Reason == CompletionTriggerReason.Deletion)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            ITextSnapshot snapshot = triggerLocation.Snapshot;
            if (triggerLocation.Position > snapshot.Length)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }
            CompletionDescriptor descriptor = Requester.CompletionDescriptor;
            if (descriptor == null)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            SnapshotSpan tokenSpan = FindTokenSpanAtPosition(triggerLocation);
            var contextType = triggerLocation.Snapshot.ContentType.TypeName;
            if (!descriptor.ContentTypes.Contains(contextType)) {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }
            var word = tokenSpan.GetText();
            if (descriptor.Keywords.Contains(word))
            {
                return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
            }
            if (descriptor.LowerCaseKeywords.Contains(word.ToLower()))
            {
                return new CompletionStartData(CompletionParticipation.ProvidesItems, tokenSpan);
            }
            if (!descriptor.ResponseFirstSpace)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            if (trigger.Character == ' ')
            {
                SnapshotSpan span = new SnapshotSpan(triggerLocation.GetContainingLine().Start, triggerLocation);
                var text = span.GetText();
                if (text.Length >= 2 && text[text.Length - 1] == ' ')
                {
                    //只在第一个空格处触发
                    if (text[text.Length - 2] != ' ')
                    {
                        tokenSpan = new SnapshotSpan(snapshot, triggerLocation.Position, 0);
                        return new CompletionStartData(CompletionParticipation.ExclusivelyProvidesItems, tokenSpan);
                    }
                }
            }
            return CompletionStartData.DoesNotParticipateInCompletion;
        }

        private CompletionRequestDescriptor CreateRequestDescriptor(SnapshotPoint triggerLocation, out SnapshotSpan tokenSpan)
        {
            tokenSpan = FindTokenSpanAtPosition(triggerLocation);
            CompletionRequestDescriptor payload = new()
            {
                ContentType = triggerLocation.Snapshot.ContentType.TypeName,
                Word = tokenSpan.GetText(),
                Position = triggerLocation.Position,
                //LinePosition = triggerLocation.Position - triggerLocation.GetContainingLine().Start.Position,
                //LineNumber = triggerLocation.GetContainingLine().LineNumber,
                //Line = triggerLocation.GetContainingLine().GetText()
            };
            return payload;
        }

        private SnapshotSpan FindTokenSpanAtPosition(SnapshotPoint triggerLocation)
        {
            // This method is not really related to completion,
            // we mostly work with the default implementation of ITextStructureNavigator 
            // You will likely use the parser of your language
            ITextStructureNavigator navigator = StructureNavigatorSelector.GetTextStructureNavigator(triggerLocation.Snapshot.TextBuffer);
            TextExtent extent = navigator.GetExtentOfWord(triggerLocation);
            if (triggerLocation.Position > 0 && (!extent.IsSignificant || !extent.Span.GetText().Any(c => char.IsLetterOrDigit(c))))
            {
                // Improves span detection over the default ITextStructureNavigation result
                extent = navigator.GetExtentOfWord(triggerLocation - 1);
            }

            var tokenSpan = triggerLocation.Snapshot.CreateTrackingSpan(extent.Span, SpanTrackingMode.EdgeInclusive);

            var snapshot = triggerLocation.Snapshot;
            var tokenText = tokenSpan.GetText(snapshot);
            if (string.IsNullOrWhiteSpace(tokenText))
            {
                // The token at this location is empty. Return an empty span, which will grow as user types.
                return new SnapshotSpan(triggerLocation, 0);
            }

            // Trim quotes and new line characters.
            int startOffset = 0;
            int endOffset = 0;

            if (tokenText.Length > 0)
            {
                if (tokenText.StartsWith("\""))
                    startOffset = 1;
            }
            if (tokenText.Length - startOffset > 0)
            {
                if (tokenText.EndsWith("\"\r\n"))
                    endOffset = 3;
                else if (tokenText.EndsWith("\r\n"))
                    endOffset = 2;
                else if (tokenText.EndsWith("\"\n"))
                    endOffset = 2;
                else if (tokenText.EndsWith("\n"))
                    endOffset = 1;
                else if (tokenText.EndsWith("\""))
                    endOffset = 1;
            }

            return new SnapshotSpan(tokenSpan.GetStartPoint(snapshot) + startOffset, tokenSpan.GetEndPoint(snapshot) - endOffset);
        }

        private List<SnapshotSpan> GetWords(SnapshotPoint triggerLocation)
        {
            ITextStructureNavigator navigator = StructureNavigatorSelector.GetTextStructureNavigator(triggerLocation.Snapshot.TextBuffer);
            SnapshotSpan span = new SnapshotSpan(triggerLocation.GetContainingLine().Start, triggerLocation);
            SnapshotPoint start = span.Start;
            SnapshotPoint end = span.End;

            List<SnapshotSpan> words = new List<SnapshotSpan>();
            while (start < end)
            {
                TextExtent extent = navigator.GetExtentOfWord(start);
                if (extent.IsSignificant)
                {
                    words.Add(extent.Span);
                }
                start = extent.Span.End;
            }
            return words;
        }

        private ImageElement FindImageElementByImageMonikerId(int imageMonikerId)
        {
            if (ImageElements.TryGetValue(imageMonikerId, out var imageElement))
            {
                return imageElement;
            }
            ImageMoniker moniker = default(ImageMoniker);
            moniker.Guid = KnownImageIds.ImageCatalogGuid;
            moniker.Id = imageMonikerId;
            ImageElement image = new ImageElement(moniker.ToImageId());
            ImageElements[imageMonikerId] = image;
            return image;
        }
    }
}
