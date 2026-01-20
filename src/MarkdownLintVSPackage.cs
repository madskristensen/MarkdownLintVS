global using System;
global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using Task = System.Threading.Tasks.Task;
using System.Runtime.InteropServices;
using MarkdownLintVS.Options;

namespace MarkdownLintVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.MarkdownLintVSString)]
    [ProvideOptionPage(typeof(OptionsProvider.RuleOptionsPage), Vsix.Name, "Rules", 0, 0, true, ProvidesLocalizedCategoryName = false, SupportsProfiles = true)]
    public sealed class MarkdownLintVSPackage : ToolkitPackage
    {
    }
}
