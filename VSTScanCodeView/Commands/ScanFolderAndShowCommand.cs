namespace VSTScanCodeView
{
    [Command(PackageIds.ScanFolderAndShowCommandButton)]
    internal sealed class ScanFolderAndShowCommand : BaseCommand<ScanFolderAndShowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            //await VS.MessageBox.ShowWarningAsync("ScanFolderAndShowCommand", "Button clicked");

            await ResultViewToolWindow.ShowAsync();

            await VS.Commands.ExecuteAsync(new System.ComponentModel.Design.CommandID(
                PackageGuids.VSTScanCodeView,
                PackageIds.ScanFolderCommandButton
            ));
        }
    }
}
