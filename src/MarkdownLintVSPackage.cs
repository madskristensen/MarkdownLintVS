global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using System.Threading;
using MarkdownLintVS.Commands;
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
        termValues: ["ActiveEditorContentType:markdown", "ActiveEditorContentType:vs-markdown"])]
    [ProvideAutoLoad(PackageGuids.MarkdownFileOpenString, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class MarkdownLintVSPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await Formatting.InitializeAsync();
        }
    }
}
