using Microsoft.VisualStudio.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VSTScanCodeView
{
    public class ResultViewToolWindow : BaseToolWindow<ResultViewToolWindow>
    {
        public override string GetTitle(int toolWindowId) => "TScanCode代码扫描结果";

        public override Type PaneType => typeof(Pane);

        public override async Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            Messager messager = await Package.GetServiceAsync<Messager,Messager>();
            return new ResultViewToolWindowControl(messager);
        }

        [Guid("c3384f58-c9d8-4538-8820-bb8b2ea38f9a")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
                ToolBar = new System.ComponentModel.Design.CommandID(
                    PackageGuids.VSTScanCodeView,
                    PackageIds.ScanCodeWindowToolbar
                    );
            }
        }
    }
}