using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System.IO;

namespace VSTScanCodeView
{
    [Command(PackageIds.ViewResultCommand)]
    internal sealed class ResultViewToolWindowCommand : BaseCommand<ResultViewToolWindowCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            //var location = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //if (location != null)
            //{
            //    location += "\\Resources\\tscancode\\tscancode.exe";
            //    if(File.Exists(location))
            //    {
            //        VS.StatusBar.ShowMessageAsync("找到内置的tscancode!").FireAndForget();
            //        ScanCodeAsync(location).FireAndForget();
            //    }
            //}
            return ResultViewToolWindow.ShowAsync();
        }

        //async Task ScanCodeAsync(string appLocation)
        //{
        //    var doc = await VS.Documents.GetActiveDocumentViewAsync();
        //    if (doc != null)
        //    {
        //        var file = doc.TextBuffer.GetFileName();
        //        if(file != null)
        //        {
        //            string arguments = " --xml -q " + file;
        //            var result = await  ProcessAsyncHelper.ExecuteShellCommandAsync(appLocation, arguments,-1);
        //            var issues = ScanResult.LoadFromXMLString(result.Output);
        //            VS.MessageBox.ShowAsync("扫描结果", result.Output).FireAndForget();
        //        }    
        //    }
        //}
    }
}
