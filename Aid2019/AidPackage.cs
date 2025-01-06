using Aid.Shared;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Aid
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.AidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
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
            if (obj != null)
            {
                VS.StatusBar.ShowMessageAsync($"打开工程 {obj.Name}").FireAndForget();
            }
        }
    }
}