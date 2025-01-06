using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Linq;

namespace Aid.Shared.Intellisense
{
    internal class AsyncCompletionCommitManager : IAsyncCompletionCommitManager
    {
        ImmutableArray<char> commitChars = new char[] { ' ', '\'', '"', ',', '.', ';', ':' }.ToImmutableArray();

        public IEnumerable<char> PotentialCommitCharacters => commitChars;

        public bool ShouldCommitCompletion(IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
        {
            // This method runs synchronously, potentially before CompletionItem has been computed.
            // The purpose of this method is to filter out characters not applicable at given location.

            // This method is called only when typedChar is among the PotentialCommitCharacters
            // in this simple example, all PotentialCommitCharacters do commit, so we always return true
            return true;
        }

        public CommitResult TryCommit(IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
        {
            //如果存在定制的提交要求,则需要按照要求提交
            if (item.Properties.TryGetProperty<Shared.CompletionSuggestedItem>(nameof(Shared.CompletionSuggestedItem), out var matchingItem))
            {
                if (matchingItem.Commits != null && matchingItem.Commits.Any())
                {
                    var span = session.ApplicableToSpan;
                    int length = 0;
                    using (var edit = buffer.CreateEdit())
                    {
                        //越靠后的越先插入
                        var commits = matchingItem.Commits;
                        commits.Sort((lhs, rhs) => lhs.StartPosition.CompareTo(rhs.StartPosition));

                        foreach (var commit in commits)
                        {
                            if (commit.EndPosition > commit.StartPosition)
                            {
                                if (edit.Replace(new Span(commit.StartPosition, commit.EndPosition - commit.StartPosition), commit.Text))
                                {
                                    length += commit.Text.Length - (commit.EndPosition - commit.StartPosition);
                                }
                            }
                            else
                            {
                                if (edit.Insert(commit.StartPosition, commit.Text))
                                {
                                    length += commit.Text.Length;
                                }
                            }
                        }
                        edit.Apply();
                    }
                    ////定位到最靠后的提交位置
                    //var newPosition = span.GetStartPoint(buffer.CurrentSnapshot).Position +length;
                    //session.TextView.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, newPosition));
                    return CommitResult.Handled;
                }
            }
            return CommitResult.Unhandled; // use default commit mechanism.
        }
    }
}
