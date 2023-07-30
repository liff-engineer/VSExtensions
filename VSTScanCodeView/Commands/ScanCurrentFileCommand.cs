using Microsoft.VisualStudio.Text;

namespace VSTScanCodeView
{
    [Command(PackageIds.ScanCurrentFileCommandButton)]
    internal sealed class ScanCurrentFileCommand : BaseCommand<ScanCurrentFileCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            //await VS.MessageBox.ShowWarningAsync("ScanCurrentFileCommand", "Button clicked");
            Messager messager = await Package.GetServiceAsync<Messager,Messager>();
            if (messager != null)
            {
                var docView = await VS.Documents.GetActiveDocumentViewAsync();
                if (docView != null)
                {
                    var file = docView.TextBuffer.GetFileName();
                    if (file != null)
                    {
                        messager.Send(new ScanFileAction() { 
                            file= file,
                        });
                    }
                }
            }
        }
    }
}
