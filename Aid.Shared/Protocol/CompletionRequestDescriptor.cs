using System.Collections.Generic;

namespace Aid.Shared
{
    public class CompletionRequestDescriptor
    {
        public string ContentType { get; set; }
        public string Word { get; set; }
        public List<string> Words { get; set; } 
        public string Content { get; set; }
        public int Position { get; set; } = 0;
        
        public CompletionRequestDescriptor() { }
    }
}
