using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aid.Shared.Implement
{
    internal class ProcessCommandRequester : IRequester
    {
        public struct Result
        {
            public int? ExitCode;
            public string Output;
            public string Error;
        }

        public string Identity { get; }
        public Dictionary<string,ProcessDescriptor> Commands  = new Dictionary<string, ProcessDescriptor> ();
        private Requester Requester { get; }
        public ProcessCommandRequester(string identity, Dictionary<string, ProcessDescriptor> commands)
        {
            Requester = Requester.Instance;
            Identity = identity;
            Commands = commands;
        }

        public Task<object> InitializeAsync()
        {
            return Task.FromResult<object>(null);
        }

        public async Task<object> ExecuteAsync(string topic, object option, object argument, CancellationToken cancellationToken)
        {
            if(topic == null) throw new ArgumentNullException(nameof(topic));
            if (topic == "Commands")
            {
                List<CommandDescriptor> descriptors = new List<CommandDescriptor>();
                foreach (var obj in Commands)
                {
                    descriptors.Add(new CommandDescriptor()
                    {
                        Identity = obj.Key,
                        Label = obj.Value.Label,
                        Group = obj.Value.Group,
                        Description = obj.Value.Description,
                    });
                }
                return descriptors;
            }
            else if(Commands.TryGetValue(topic, out var Descriptor))
            {
                int timeout = -1;
                using (var Process = new Process())
                {
                    Process.StartInfo = Descriptor.StartInfo;
                    List<string> args = new List<string>();
                    if(Descriptor.Arguments != null)
                    {
                        foreach (var arg in Descriptor.Arguments)
                        {
                            args.Add(Requester.ReplaceVariables(arg,Descriptor.Variables));
                        }
                    }
                    if (args.Count > 0) {
                        Process.StartInfo.Arguments = string.Join(", ", args);  
                    }

                    Requester.Logger.Info($"执行命令:{Process.StartInfo.FileName} {Process.StartInfo.Arguments}");
                    Requester.NotifyStatusChanged($"[Aid]执行命令:{Process.StartInfo.FileName} {Process.StartInfo.Arguments}");

                    Process.StartInfo.UseShellExecute = false;
                    Process.StartInfo.CreateNoWindow = Descriptor.IsBackground;
                    if (Descriptor.IsBackground)
                    {
                        Process.StartInfo.RedirectStandardOutput = true;
                        Process.StartInfo.RedirectStandardError = true;
                        var outputBuiler = new StringBuilder();
                        var errorBuiler = new StringBuilder();
                        var outputCloseEvent = new TaskCompletionSource<bool>();
                        var errorCloseEvent = new TaskCompletionSource<bool>();

                        Process.OutputDataReceived += (sender, args) =>
                        {
                            if (args.Data != null) { outputBuiler.AppendLine(args.Data); }
                            else { outputCloseEvent.SetResult(false); }
                        };
                        Process.ErrorDataReceived += (sender, args) =>
                        {
                            if (args.Data != null) { errorBuiler.AppendLine(args.Data); }
                            else { errorCloseEvent.SetResult(false); }
                        };
                        try
                        {
                            Process.Start();
                        }
                        catch (Exception error)
                        {
                            return new Result
                            {
                                ExitCode = -1,
                                Error = error.Message,
                            };
                        }
                        Process.BeginOutputReadLine();
                        Process.BeginErrorReadLine();

                        var waitForExit = Task.Run(() => { return Process.WaitForExit(timeout); });
                        var task = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

                        if (await Task.WhenAny(Task.Delay(timeout), task) == task && await waitForExit)
                        {
                            return new Result
                            {
                                ExitCode = Process.ExitCode,
                                Output = outputBuiler.ToString(),
                                Error = errorBuiler.ToString(),
                            };
                        }
                        else
                        {
                            try
                            {
                                Process.Kill();
                            }
                            catch { }
                        }

                    }
                    else
                    {
                        try
                        {
                            Process.Start();
                        }
                        catch (Exception error)
                        {
                            return new Result
                            {
                                ExitCode = -1,
                                Error = error.Message,
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void Dispose()
        {
        }
    }
}
