using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace VSTScanCodeView
{
    public partial class ResultViewToolWindowControl : UserControl
    {
        public Messager Messager = null;
        public string AppLocation = null;
        private List<IssueItem> issues = new List<IssueItem>();
        private string RootPath = null;

        public ResultViewToolWindowControl(Messager messager)
        {
            InitializeComponent();
            messager ??= new Messager();
            Messager = messager;
            Messager.MessageReceived += Messager_MessageReceived;

            var location = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (location != null)
            {
                location += "\\Resources\\tscancode\\tscancode.exe";
                if (File.Exists(location))
                {
                    AppLocation = location;
                }
            }
        }

        private void Messager_MessageReceived(object sender, object e)
        {
            ScanFileAction scanFileAction = e as ScanFileAction;
            if (scanFileAction != null)
            {
                ScanAnyLoadIssuesAsync(scanFileAction.file,false).FireAndForget();
            }
            ScanDirectoryAction scanDirectoryAction = e as ScanDirectoryAction;
            if (scanDirectoryAction != null)
            {
                ScanAnyLoadIssuesAsync(scanDirectoryAction.directory, true).FireAndForget();
            }
        }

        private async Task ScanAnyLoadIssuesAsync(string path,bool isDirectory)
        {
            var command = AppLocation;
            var options = await General.GetLiveInstanceAsync();
            if (options != null)
            {
                if(options.ApplicationPath != null)
                {
                    if (File.Exists(options.ApplicationPath))
                    {
                        command = options.ApplicationPath;
                    }
                }
            }
            if (command != null)
            {
                VS.StatusBar.ShowMessageAsync($"使用{command}进行代码扫描").FireAndForget();
                string arguments = " --xml -q " + path;

                var result = await ProcessAsyncHelper.ExecuteShellCommandAsync(command, arguments, -1);
                VS.StatusBar.ShowMessageAsync($"{path}代码扫描完成").FireAndForget();

                issues = ScanResult.LoadFromXMLString(result.Output).errors;
                if(issues == null)
                {
                    VS.MessageBox.ShowErrorAsync("无法解析的输出", result.Output).FireAndForget();
                    return;
                }
                for(int i = 0;i < issues.Count;i++)
                {
                    var item = issues[i];
                    if (!isDirectory)
                    {
                        //如果是检查的当前文件，则只保留文件名
                        item.File = Path.GetFileName(item.File);
                    }
                    else
                    {
                        //如果检查的是目录，则文件名前面的目录全部移除
                        item.File = item.File.Replace(path,"");
                    }
                }
                if (isDirectory)
                {
                    RootPath = path;
                }
                else
                {
                    RootPath = Path.GetDirectoryName(path)+"\\";
                }

                Issues.ItemsSource = issues;
            }
            else
            {
                VS.MessageBox.ShowErrorAsync("未找到TScanCode.exe,无法进行代码扫描").FireAndForget();
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGridRow row = sender as DataGridRow; 
            if (row != null)
            {
                var issue = row.Item as IssueItem;
                if (issue != null && RootPath!= null)
                {
                    OpenIssueInEditorAsync(RootPath+issue.File,issue.Line).FireAndForget();
                }
            }
        }

        private async Task OpenIssueInEditorAsync(string file, int line)
        {
            await VS.StatusBar.ShowMessageAsync($"打开{file}并定位到{line}行");
            var options = await General.GetLiveInstanceAsync();
            var openMode = IssueFileOpenMode.OpenViaProject;
            if (options != null)
            {
                openMode = options.OpenMode;
            }
            if (openMode == IssueFileOpenMode.OpenViaProject)
            {
                await VS.Documents.OpenViaProjectAsync(file);
            }
            else
            {
                await VS.Documents.OpenAsync(file);
            }
            var docView = await VS.Documents.GetDocumentViewAsync(file);
            if(docView != null) { 
                var textView = docView.TextView; 
                if (textView != null && line > 0)
                {
                    var textLine = textView.TextSnapshot.GetLineFromLineNumber(line - 1);
                    //移动光标到开始行
                    textView.Caret.MoveTo(new SnapshotPoint(textView.TextSnapshot, textLine.Start.Position));
                    //选中目标行
                    var lineSpan = new SnapshotSpan(textLine.Start, textLine.Length);
                    textView.Selection.Select(lineSpan, false);
                    //视图移动
                    textView.ViewScroller.EnsureSpanVisible(lineSpan, EnsureSpanVisibleOptions.AlwaysCenter);
                }
            }
        }
    }
}