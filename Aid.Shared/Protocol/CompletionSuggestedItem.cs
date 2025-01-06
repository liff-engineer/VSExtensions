using System.Collections.Generic;

namespace Aid.Shared
{
    public class CompletionCommit
    {
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public string Text { get; set; }    
    }

    public class CompletionSuggestedItem
    {
        public string DisplayText { get; set; }
        public string InsertText { get; set; }
        public string SortText { get; set; }
        public string FilterText { get; set; }
        public string Suffix { get; set; }
        public string Description { get; set; }
        public int ImageMonikerId { get; set; } = 0;
        public List<CompletionCommit> Commits { get; set; }
    }
}
