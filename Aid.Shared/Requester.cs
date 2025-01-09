using Aid.Shared.Implement;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aid.Shared
{
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    public delegate void StatusEventHandler(object sender,StatusEventArgs args);

    public sealed class Requester
    {
        public const string Name = "Aid";
        public const string ConfigureFile = "Aid.json";

        private static readonly Lazy<Requester> lazyInstance = new Lazy<Requester>(() => new Requester());

        private FileSystemWatcher userHomeWatcher;
        private FileSystemWatcher workspaceFolderWatcher;
        private string UserHome;
        private string WorkspaceFolder;

        public event StatusEventHandler statusChanged;

        public void NotifyStatusChanged(string message)
        {
            statusChanged?.Invoke(this, new StatusEventArgs() {Message = message });
        }

        private Requester() {
            UserHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Name);
            Logger = new Logger(Path.Combine(UserHome,$"{Name}.log"));
            Variables["userHome"] = UserHome;
        }

        public static Requester Instance
        {
            get { return lazyInstance.Value; }
        }

        public ILogger Logger { get; }

        public CompletionDescriptor CompletionDescriptor { get; set; } = new CompletionDescriptor();

        public List<IRequester> Requesters { get; set; } = new List<IRequester>();

        public Dictionary<string, Object> Variables { get; set; } = new Dictionary<string, object>();

        public static string ReplaceVariables(string original,Dictionary<string,Object> variables)
        {
            string pattern = @"\$\{(\w+)\}";
            bool contains = Regex.IsMatch(original, pattern);
            if (contains)
            {
                return Regex.Replace(original, pattern, match => {
                    string variableName = match.Groups[1].Value;
                    if (variables.TryGetValue(variableName, out var value))
                    {
                        return value.ToString();
                    }
                    return match.Value;
                });
            }
            else
            {
                return original;
            }
        }

        private void Initialize()
        {
            if(Requesters.Count != 0)
            {
                foreach(IRequester requester in Requesters)
                {
                    requester.Dispose();
                }
                Requesters.Clear();
            }
            Requesters.Add(new InspectRequester(Name));
            Load(RequesterKind.User,UserHome);
            Load(RequesterKind.Workspace, WorkspaceFolder);
        }

        public static void ParseResponse<T>(Object response,ref List<T> results)
        {
            if (response == null) return;
            if (response is T obj)
            {
                results.Add(obj);
            }
            else if (response is List<T> objects)
            {
                results.AddRange(objects);
            }
            else if (response is string jsonStr)
            {
                ParseResponse<T>(JObject.Parse(jsonStr),ref results);
            }
            else if (response is JArray jsonArray)
            {
                foreach (var j in jsonArray)
                {
                    ParseResponse<T>(j, ref results);
                }
            }
            else if (response is JObject json)
            {
                results.Add(json.ToObject<T>());
            }
        }

        public async Task InitializeAsync()
        {
            foreach (IRequester requester in Requesters)
            {
                await requester.InitializeAsync();
            }
            CompletionDescriptor = new CompletionDescriptor();
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
            {
                CancellationToken token = cts.Token;
                try
                {
                    var  descriptors = await ExecuteAsync<CompletionDescriptor>("Completions", null, null, token);
                    foreach (var descriptor in descriptors)
                    {
                        CompletionDescriptor.Merge(descriptor);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"发生错误 ${ex.Message}");
                }
            }
        }

        private void Load(RequesterKind Kind,string folder)
        {
            if (folder == null) return;

            string prefix = folder == UserHome ? "User." : "Workspace.";
            string file = Path.Combine(folder, ConfigureFile);
            if (!File.Exists(file)) { return; }

            //移除原有Requester
            foreach (IRequester requester in Requesters)
            {
                if (requester.Kind != Kind) continue;
                requester.Dispose();
            }
            Requesters.RemoveAll(requester=>requester.Kind == Kind);

            List<ProcessDescriptor> taskDescriptiors = ProcessDescriptor.Load(file, "Tasks");
            Dictionary<string, ProcessDescriptor> commands = new Dictionary<string, ProcessDescriptor> { };
            foreach (var descriptor in taskDescriptiors)
            {
                descriptor.Variables = new Dictionary<string, object>(Variables);
                descriptor.Variables.Add("fileFolder", folder);
                commands[Guid.NewGuid().ToString()] = descriptor;
            }
            Requesters.Add(new ProcessCommandRequester(Kind,$"{prefix}Tasks", commands));
            List<ProcessDescriptor> processDescriptors = ProcessDescriptor.Load(file, "Services");
            foreach (var descriptor in processDescriptors)
            {
                descriptor.Variables = new Dictionary<string, object>(Variables);
                descriptor.Variables.Add("fileFolder", folder);
                Requesters.Add(new ProcessRequester(Kind, $"{prefix}Service." + Guid.NewGuid().ToString(), descriptor));
            }
        }

        public static async Task<List<Object>> ExecuteAsync(string topic,Object option,Object argument,CancellationToken cancellationToken)
        {
            Requester Requester = Requester.Instance;
            try
            {
                List<Task<Object>> requests = new List<Task<object>>();
                foreach (var requester in Requester.Requesters)
                {
                    var task = requester.ExecuteAsync(topic, option, argument, cancellationToken);
                    if (task != null)
                    {
                        requests.Add(task);
                    }
                }
                return new List<Object>(await Task.WhenAll(requests));
            }
            catch (AggregateException ex)
            {
                foreach (var exception in ex.InnerExceptions)
                {
                    if (exception is OperationCanceledException)
                    {
                        Requester.Logger.Error($"一个或多个任务被取消。");
                    }
                    else
                    {
                        Requester.Logger.Error($"发生其他异常:{exception.Message}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Requester.Logger.Error($"任务被取消。");
            }
            catch (Exception ex)
            {
                Requester.Logger.Error($"发生其他异常: {ex.Message}");
            }
            return null;
        }
        public static async Task<List<T>> ExecuteAsync<T>(string topic, Object option, Object argument, CancellationToken token)
        {
            var objects = await ExecuteAsync(topic, option, argument, token);
            List<T> results = new List<T>();
            if (objects == null) return results;
            foreach (var obj in objects)
            {
                ParseResponse<T>(obj, ref results);
            }
            return results;
        }

        public void InitialzeAfterSolutionReady(string solutionFullPath)
        {
            InitializeUserFileWatcher();    

            DirectoryInfo path = new DirectoryInfo(solutionFullPath);
            path = path.Parent;
            FileInfo file = null;
            while (path.Parent != null)
            {
                var files = path.GetFiles(ConfigureFile,SearchOption.TopDirectoryOnly);
                if(files.Length > 0)
                {
                    file = files[0];
                    break;
                }
                path = path.Parent;
            }
            if (file != null)
            {
                WorkspaceFolder = Path.GetDirectoryName(file.FullName);
                Variables["workspaceFolder"] = WorkspaceFolder;

                NotifyStatusChanged($"[Aid]识别到的工作区为 {WorkspaceFolder}。");
                InitialzieWorkspaceFileWatcher();
            }
            Initialize();
        }

        private void InitializeUserFileWatcher()
        {
            var UserConfigureFile = Path.Combine(UserHome, ConfigureFile);
            FileInfo fileInfo = new FileInfo(UserConfigureFile);
            if (!fileInfo.Exists)
            {
                Logger.Warn($"未从用户目录下找到配置文件 {UserConfigureFile}");
                return;
            }

            userHomeWatcher = new FileSystemWatcher();
            userHomeWatcher.Path = fileInfo.Directory.FullName;
            userHomeWatcher.Filter = fileInfo.Name;
            userHomeWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite;
            userHomeWatcher.Changed += UserHomeWatcher_Changed;
            userHomeWatcher.EnableRaisingEvents = true;
        }

        private void InitialzieWorkspaceFileWatcher()
        {
            var workspaceConfigureFile = Path.Combine(WorkspaceFolder, ConfigureFile);
            FileInfo fileInfo = new FileInfo(workspaceConfigureFile);
            if (!fileInfo.Exists)
            {
                Logger.Warn($"未从工作区 {WorkspaceFolder} 下找到配置文件 {ConfigureFile}");
                return;
            }

            workspaceFolderWatcher = new FileSystemWatcher();
            workspaceFolderWatcher.Path = fileInfo.Directory.FullName;
            workspaceFolderWatcher.Filter = fileInfo.Name;
            workspaceFolderWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite;
            workspaceFolderWatcher.Changed += WorkspaceFolderWatcher_Changed;
            workspaceFolderWatcher.EnableRaisingEvents = true;
        }

        private void WorkspaceFolderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            Logger.Info($"配置文件 {e.FullPath} 发生变化，重新加载。");
            NotifyStatusChanged($"[Aid]配置文件 {e.FullPath} 发生变化，重新加载。");
            Load(RequesterKind.Workspace, WorkspaceFolder);
        }

        private void UserHomeWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            Logger.Info($"配置文件 {e.FullPath} 发生变化，重新加载。");
            NotifyStatusChanged($"[Aid]配置文件 {e.FullPath} 发生变化，重新加载。");
            Load(RequesterKind.User,UserHome);
        }
    }
}
