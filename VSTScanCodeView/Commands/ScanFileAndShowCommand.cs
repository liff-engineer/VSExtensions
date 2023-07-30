namespace VSTScanCodeView
{
    [Command(PackageIds.ScanFileAndShowCommandButton)]
    internal sealed class ScanFileAndShowCommand : BaseCommand<ScanFileAndShowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            //await VS.MessageBox.ShowWarningAsync("ScanFileCommand", "Button clicked");
            //await VS.Commands.ExecuteAsync(PackageIds.ScanCurrentFileCommandButton);

            await ResultViewToolWindow.ShowAsync();

            await VS.Commands.ExecuteAsync(new System.ComponentModel.Design.CommandID(
                    PackageGuids.VSTScanCodeView,
                    PackageIds.ScanCurrentFileCommandButton
                    ));
        }
    }
}
