using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Aid.Shared.Implement
{
    internal class ProcessRequester:IRequester
    {
        private static int RequestId = 0;

        public string Identity { get; }

        public ProcessDescriptor Descriptor { get; set; }

        private Requester Requester { get; }

        private Process  Process { get; set; }

        private List<string> Topics { get; set; } = new List<string>() { "Topics"};

        public RequesterKind Kind { get; }

        public ProcessRequester(RequesterKind kind,string identity, ProcessDescriptor descriptor)
        {
            Requester = Requester.Instance;
            Kind = kind;
            Identity = identity;
            Descriptor = descriptor;
        }

        public async Task<object> InitializeAsync()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
            {
                CancellationToken token = cts.Token;
                try
                {
                    var obj = await ExecuteAsync("Topics", null, null, token);
                    if(obj is List<string>)
                    {
                        Topics = obj as List<string>;
                        Topics.Add("Topics");
                    }
                    else if(obj is JArray)
                    {
                        Topics.Clear();
                        foreach(var item in (obj as JArray))
                        {
                            Topics.Add(item.ToString());
                        }
                        Topics.Add("Topics");
                    }
                    else if(obj != null)
                    {
                        Requester.Logger.Error($"无法识别的反馈内容： {obj.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    Requester.Logger.Error($"发生错误 ${ex.Message}");
                }
            }
            return null;
        }

        public async Task<object> ExecuteAsync(string topic, object option, object argument, CancellationToken cancellationToken)
        {
            if (!Topics.Contains(topic)) return null;

            if (Process == null)
            {
                Process = new Process
                {
                    StartInfo = Descriptor.StartInfo
                };
                List<string> args = new List<string>();
                if (Descriptor.Arguments != null)
                {
                    foreach (var arg in Descriptor.Arguments)
                    {
                        args.Add(Requester.ReplaceVariables(arg, Descriptor.Variables));
                    }
                }
                if (args.Count > 0)
                {
                    Process.StartInfo.Arguments = string.Join(" ", args);
                }

                Requester.Logger.Info($"启动服务:{Process.StartInfo.FileName} {Process.StartInfo.Arguments}");

                Process.StartInfo.UseShellExecute = false;
                Process.StartInfo.CreateNoWindow = true;
                Process.StartInfo.RedirectStandardInput = true;
                Process.StartInfo.RedirectStandardOutput = true;
                Process.Start();
            }
            else if (Process.HasExited)
            {
                Process.Start();
            }

            if (Process == null) throw new ArgumentNullException(nameof(Process));
            var lastRequestId = RequestId;
            RequestId += 1;
            JObject request = new JObject() {
                {"Topic",topic },
                {"Id", lastRequestId }
            };
            if (argument != null)
            {
                if(argument is JObject)
                {
                    request.Add("Argument",argument as JObject);    
                }
                else
                {
                    Requester.Logger.Error("argument 参数需要是JObject对象");
                }
            }

            if(request != null)
            {
                var message = request.ToString(Newtonsoft.Json.Formatting.None);
                await Process.StandardInput.WriteLineAsync(message);
                Requester.Logger.Info($"发送请求 {message}");
            }

            List<string> Buffers = new List<string>();
            while (true)
            {
                try
                {
                    Task<string> task = Process.StandardOutput.ReadLineAsync();
                    Task completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cancellationToken));
                    if(completedTask == task)
                    {
                        string response = await task;
                        if (response == null)
                            continue;
                        Buffers.Add(response.Trim());

                        try
                        {
                            JObject rep = JObject.Parse(string.Join("\n", Buffers));
                            if(rep == null)
                            {
                                Requester.Logger.Error($"反馈内容为空");
                                continue;
                            }

                            if(rep.TryGetValue("Id",out var id))
                            {
                                if(id.ToObject<int>()!= lastRequestId)
                                {
                                    Requester.Logger.Error($"反馈ID不匹配：预期 {lastRequestId},实际 {id.ToObject<int>()} ");
                                    continue;
                                }
                            }
                            if (rep.TryGetValue("Error", out var error))
                            {
                                var errorMessage = error.ToString();
                                Requester.Logger.Error($"反馈错误：{errorMessage}");
                                return null;
                            }
                            if(rep.TryGetValue("Result",out var result))
                            {
                                Requester.Logger.Info($"接收到有效反馈: {result.ToString(Formatting.None)}");
                                return result;
                            }
                            Requester.Logger.Error($"接收到反馈: {rep.ToString(Formatting.None)}");
                            return null;
                        }
                        catch(JsonReaderException)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Requester.Logger.Info("读取操作被取消。");
                            return null;
                        }
                        Requester.Logger.Info("未知错误。");
                        return null;
                    }
                }
                catch(OperationCanceledException)
                {
                    Requester.Logger.Info("操作因OperationCanceledException 被取消。");
                    return null;
                }
                catch(Exception ex)
                {
                    Requester.Logger.Info($"发生异常: {ex.Message}");
                    return null;
                }
            }

        }

        public void Dispose()
        {
            if (Process != null)
            {
                if (!Process.HasExited)
                {
                    if (!Process.CloseMainWindow())
                    {
                        Process.StandardInput.Close();
                    }

                    //等待2秒，如果不正常退出则强制关闭
                    if (!Process.WaitForExit(2000))
                    {
                        try
                        {
                            Process.Kill();
                            Console.WriteLine($"进程 {Process.ProcessName} 已被强制关闭。");
                            Requester.Logger.Info($"进程 {Process.ProcessName} 已被强制关闭。");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"强制关闭进程时发生错误：${ex.Message}");
                            Requester.Logger.Error($"强制关闭进程 {Process.ProcessName} 时发生错误：${ex.Message}。");
                        }
                    }
                    Process.Close();
                }
            }
        }
    }
}
