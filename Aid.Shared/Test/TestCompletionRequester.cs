using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aid.Shared.Test
{
    internal class TestCompletionRequester : IRequester
    {
        public string Identity { get; set; }

        public TestCompletionRequester()
        {
            Identity = "TestCompletionDescriptor";
        }

        public void Dispose()
        {
            
        }

        public static CompletionDescriptor CreateDescriptor()
        {
            CompletionDescriptor descriptor = new CompletionDescriptor();
            descriptor.ContentTypes.Add("C/C++");
            descriptor.Keywords.Add("GUID");
            descriptor.Keywords.Add("IMPORT");
            descriptor.LowerCaseKeywords.Add("dbg");
            descriptor.ResponseFirstSpace = true;
            return descriptor;
        }

        public Task<object> InitializeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<object> ExecuteAsync(string topic, object option, object argument, CancellationToken cancellationToken)
        {
            if(topic == "CompletionSuggestedItems")
            {
                if (argument is CompletionRequestDescriptor)
                {
                    var request = argument as CompletionRequestDescriptor;

                    List<CompletionSuggestedItem> suggestedItems = new();
                    if (request.Word == "GUID")
                    {
                        suggestedItems.Add(new CompletionSuggestedItem()
                        {
                            DisplayText = "生成GUID",
                            InsertText = "//" + Guid.NewGuid().ToString(),
                            Description = "生成新的GUID，并插入到目标位置",
                            ImageMonikerId = 1385
                        });
                    }
                    if (request.Word.ToLower() == "dbg")
                    {
                        suggestedItems.Add(new CompletionSuggestedItem()
                        {
                            DisplayText = "生成调试警告",
                            InsertText = "//" + Guid.NewGuid().ToString(),
                            Description = "生成调试警告，并插入到目标位置",
                            ImageMonikerId = 3931
                        });
                    }
                    if (request.Word == "IMPORT")
                    {
                        List<CompletionCommit> commits = new List<CompletionCommit>();
                        commits.Add(new CompletionCommit()
                        {
                            StartPosition = 0,
                            EndPosition = 0,
                            Text = $"#include <string> \r\n#include <vector>\r\n"
                        });
                        commits.Add(new CompletionCommit()
                        {
                            StartPosition = request.Position - "IMPORT".Length,
                            EndPosition = request.Position,
                            Text = "std::string strVal{};"
                        });
                        suggestedItems.Add(
                            new CompletionSuggestedItem()
                            {
                                DisplayText = "导入标准库",
                                Description = "导入标准库",
                                ImageMonikerId = 1385,
                                Commits = commits
                            }
                            );
                    }
                    if (string.IsNullOrWhiteSpace(request.Word))
                    {
                        foreach (var item in request.Words)
                        {
                            suggestedItems.Add(new CompletionSuggestedItem()
                            {
                                DisplayText = item.ToUpper(),
                                Description = "推荐变量命名",
                                ImageMonikerId = 3931
                            });
                        }
                    }
                    return Task.FromResult<Object>(suggestedItems);
                }
            }
            return Task.FromResult<Object>(null);
        }
    }
}
