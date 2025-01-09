using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace Aid.Shared.Intellisense
{
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Aid completion provider")]
    [ContentType("text")]
    internal class AsyncCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        IDictionary<ITextView, IAsyncCompletionSource> cache = new Dictionary<ITextView, IAsyncCompletionSource>();
        
        // Disable "Field is never assigned to..." compiler's warning. Justification: the field is assigned by MEF.
#pragma warning disable 649
        [Import]
        internal ITextStructureNavigatorSelectorService StructureNavigatorSelector { get; set; }
        [Import]
        internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }
#pragma warning restore 649

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            if (cache.TryGetValue(textView, out var itemSource))
                return itemSource;

            var source = new AsyncCompletionSource(StructureNavigatorSelector, ClassifierAggregatorService); // opportunity to pass in MEF parts
            textView.Closed += (o, e) => cache.Remove(textView); // clean up memory as files are closed
            cache.Add(textView, source);
            return source;
        }
    }
}
