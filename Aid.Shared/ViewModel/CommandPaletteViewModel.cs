using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Aid.Shared
{
    public class CommandPaletteViewModel : ObservableRecipient
    {
        public ObservableCollection<CommandDescriptor> Commands { get; } = new ObservableCollection<CommandDescriptor>();

        public IAsyncRelayCommand InitializeCommand { get; }
        public IAsyncRelayCommand ExecuteCommand { get; }
        public CommandDescriptor SelectedCommand { get; set; }

        public Requester Requester { get; }   
        public CommandPaletteViewModel()
        {
            Requester = Requester.Instance;
            InitializeCommand = new AsyncRelayCommand(InitializeAsync);
            ExecuteCommand = new AsyncRelayCommand(ExecuteAsync);
        }

        private async Task InitializeAsync()
        {
            TimeSpan timeout = TimeSpan.FromSeconds(3);
            using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
            {
                CancellationToken token = cts.Token;
                try
                {
                    var commands = await Requester.ExecuteAsync<CommandDescriptor>("Commands", null, null, token);
                    foreach(var command in commands)
                    {
                        Commands.Add(command);
                    }
                }
                catch (Exception ex)
                {
                    Requester.Logger.Error($"发生错误 ${ex.Message}");
                }
            }
        }

        private async Task ExecuteAsync()
        {
            try
            {
                if (SelectedCommand == null) return;
                //哪怕是命令执行,暂时最多等10分钟
                TimeSpan timeout = TimeSpan.FromMinutes(10);
                using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
                {
                    CancellationToken token = cts.Token;
                    try
                    {
                        var responses = await Requester.ExecuteAsync(SelectedCommand.Identity, null, null, token);
                        foreach(var response in responses)
                        {
                            //TODO FIXME 命令执行应该有统一的返回值
                        }
                    }
                    catch (Exception ex)
                    {
                        Requester.Logger.Error($"发生错误 ${ex.Message}");
                    }
                }
            }
            catch { }
        }
    }
}
