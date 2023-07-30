using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Win32;
using System.IO;

namespace VSTScanCodeView
{
    [Command(PackageIds.ScanFolderCommandButton)]
    internal sealed class ScanFolderCommand : BaseCommand<ScanFolderCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            //await VS.MessageBox.ShowWarningAsync("ScanFolderCommand", "Button clicked");
            Messager messager = await Package.GetServiceAsync<Messager, Messager>();
            if (messager != null)
            {
                //C# 没有**正常**的选择文件夹对话框，这里用打开文件模拟一下
                OpenFileDialog dlg = new OpenFileDialog();
                var obj = await VS.Solutions.GetCurrentSolutionAsync();
                if(obj != null)
                {
                    var fullPath = obj.FullPath;
                    if (fullPath != null)
                    {
                        var pathDir = Path.GetDirectoryName(fullPath);
                        if (pathDir != null)
                        {
                            dlg.InitialDirectory = pathDir;
                        }
                    }
                }
                dlg.Filter= "All Files (*.*)|*.*";
                dlg.FilterIndex = 1;
                dlg.Title = "选择要扫描的代码目录（随便选个其中的文件即可）";
                dlg.RestoreDirectory = true;
                dlg.Multiselect = false;
                var result = dlg.ShowDialog();
                if(result == true)
                {
                    var pathDir = Path.GetDirectoryName(dlg.FileName);
                    if (pathDir != null)
                    {
                        messager.Send(new ScanDirectoryAction()
                        {
                            directory = pathDir,
                        });
                    }
                }
            }
        }
    }
}
