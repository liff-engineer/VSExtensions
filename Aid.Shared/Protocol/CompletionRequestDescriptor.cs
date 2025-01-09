using System.Collections.Generic;

namespace Aid.Shared
{
    public class Classification
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public int LineNumber {  get; set; }
    }

    public class CompletionRequestDescriptor
    {
        public string ContentType { get; set; }
        public string Word { get; set; }
        public string Content { get; set; }
        public int Position { get; set; } = 0;
        public List<string> Words { get; set; } = new List<string>();
        public List<Classification> Tokens { get; set; } = new List<Classification>();

        public CompletionRequestDescriptor() { }
    }
}
