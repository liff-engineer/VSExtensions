using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aid.Shared
{
    public interface IRequester:IDisposable
    {
        public string Identity { get; }

        Task<Object> InitializeAsync();
        Task<Object> ExecuteAsync(string topic, Object option, Object argument, CancellationToken cancellationToken);
    }
}
