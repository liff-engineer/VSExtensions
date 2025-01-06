global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using Aid.Shared;
using Microsoft.VisualStudio;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Media;

namespace Aid
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.AidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string,PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class AidPackage : ToolkitPackage
    {
        private Requester Requester;
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Requester = Requester.Instance;
            Requester.statusChanged += Requester_statusChanged;
            await this.RegisterCommandsAsync();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VS.Events.SolutionEvents.OnAfterOpenProject += SolutionEvents_OnAfterOpenProject;

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            var fullPath = solution.FullPath;

            Requester.InitialzeAfterSolutionReady(fullPath);
            await Requester.InitializeAsync();
        }

        private void Requester_statusChanged(object sender, StatusEventArgs args)
        {
            VS.StatusBar.ShowMessageAsync(args.Message).FireAndForget();
        }

        private void SolutionEvents_OnAfterOpenProject(Project obj)
        {
            if (obj != null) {
                VS.StatusBar.ShowMessageAsync($"打开工程 {obj.Name}").FireAndForget();
            }
        }
    }
}