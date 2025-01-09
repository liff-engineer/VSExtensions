using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aid.Shared.Implement
{
    internal class InspectRequester : IRequester
    {
        public string Identity { get; } = "Aid";
        public RequesterKind Kind { get; } = RequesterKind.Internal;
        public InspectRequester(string identity)
        {
            Identity = identity;
        }

        public void Dispose()
        {

        }

        public Task<object> ExecuteAsync(string topic, object option, object argument, CancellationToken cancellationToken)
        {
            if(topic == null)  throw new ArgumentNullException(nameof(topic));
            if(topic == "Commands")
            {
                List<CommandDescriptor> descriptors = new List<CommandDescriptor>();
                descriptors.Add(new CommandDescriptor()
                {
                    Identity="Aid.OpenLogFile",
                    Label = "打开日志文件",
                    Group = Identity,
                    Description= "打开日志文件",
                });
                descriptors.Add(new CommandDescriptor() {
                    Identity="Aid.ReportStatus",
                    Label = "汇报内部状态",
                    Group= Identity,
                    Description="汇报内部状态"
                });
                return Task.FromResult<object>(descriptors);
            }
            if(topic == "Aid.OpenLogFile")
            {
                return  Task.FromResult<object>(null);
            }
            if(topic == "Aid.ReportStatus")
            {
                return Task.FromResult<object>(null);
            }
            return Task.FromResult<object>(null);
        }

        public Task<object> InitializeAsync()
        {
            return Task.FromResult<object>(null);
        }
    }
}
