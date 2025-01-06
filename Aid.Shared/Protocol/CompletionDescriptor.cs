using System.Collections.Generic;

namespace Aid.Shared
{
    public class CompletionDescriptor
    {
        public HashSet<string> ContentTypes {  get; set; } = new HashSet<string>();
        public HashSet<string> Keywords { get; set; } = new HashSet<string>();
        public HashSet<string> LowerCaseKeywords { get; set; } = new HashSet<string>();
        public bool ResponseFirstSpace {  get; set; }    = false;

        public CompletionDescriptor() { }

        public void Merge(CompletionDescriptor descriptor)
        {
            ContentTypes.UnionWith(descriptor.ContentTypes);
            Keywords.UnionWith(descriptor.Keywords);
            LowerCaseKeywords.UnionWith(descriptor.LowerCaseKeywords);
            if (!ResponseFirstSpace)
            {
                ResponseFirstSpace = descriptor.ResponseFirstSpace;
            }
        }
    }
}
