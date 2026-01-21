global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using MarkdownLintVS.Commands;
using MarkdownLintVS.ErrorList;
using MarkdownLintVS.Options;

namespace MarkdownLintVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.MarkdownLintVSString)]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptionsPage), Vsix.Name, "General", 0, 0, true, ProvidesLocalizedCategoryName = false, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.RuleOptionsPage), Vsix.Name, "Rules", 0, 0, true, ProvidesLocalizedCategoryName = false, SupportsProfiles = true)]
    [ProvideUIContextRule(PackageGuids.MarkdownFileOpenString,
        name: "Markdown File Open",
        expression: "markdown | vsmarkdown",
        termNames: ["markdown", "vsmarkdown"],
        termValues: [$"ActiveEditorContentType:{ContentTypes.Markdown}", $"ActiveEditorContentType:{ContentTypes.VsMarkdown}"])]
    [ProvideAutoLoad(PackageGuids.MarkdownFileOpenString, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideFileIcon(".markdownlintignore", "KnownMonikers.DocumentExclude")]
    public sealed class MarkdownLintVSPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await Formatting.InitializeAsync();
            await this.RegisterCommandsAsync();

            // Subscribe to solution/folder close events to clear error list
            VS.Events.SolutionEvents.OnAfterCloseSolution += OnSolutionClosed;
        }

        private void OnSolutionClosed()
        {
            // Clear all markdown lint errors when solution/folder is closed
            MarkdownLintTableDataSource dataSource = MarkdownLintTableDataSource.Instance;
            dataSource?.ClearAllErrors();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                VS.Events.SolutionEvents.OnAfterCloseSolution -= OnSolutionClosed;
            }
            base.Dispose(disposing);
        }
    }
}
