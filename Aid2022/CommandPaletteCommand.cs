using System.Windows;

namespace Aid
{
    [Command(PackageIds.CommandPaletteCommand)]
    internal sealed class CommandPaletteCommand : BaseCommand<CommandPaletteCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            //await VS.MessageBox.ShowWarningAsync("Aid.CommandPalette", "【2022】Button clicked");
            CommandPaletteDialogWindow window = new CommandPaletteDialogWindow();
            await window.ShowDialogAsync();
        }
    }

    [Command(PackageIds.CommandPaletteToolBarCommand)]
    internal sealed class CommandPaletteToolBarCommand : BaseCommand<CommandPaletteToolBarCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            //await VS.MessageBox.ShowWarningAsync("Aid.CommandPalette", "【2022】Button clicked");
            CommandPaletteDialogWindow window = new CommandPaletteDialogWindow();
            await window.ShowDialogAsync();
        }
    }
}
