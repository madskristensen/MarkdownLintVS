using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Command to lint Markdown files from the Open Folder (Workspace) context menu.
    /// </summary>
    [Command(PackageIds.LintOpenFolderCommand)]
    internal sealed class LintOpenFolderCommand : BaseCommand<LintOpenFolderCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                var selectedPath = await GetSelectedWorkspacePathAsync();
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    await LintFolderCommand.LintFolderAsync(selectedPath);
                }
                else
                {
                    await VS.MessageBox.ShowWarningAsync("Markdown Lint", "Could not determine the folder path. Please select a folder or file in the Folder View.");
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Markdown Lint", $"Error linting folder: {ex.Message}");
            }
        }

        private static async Task<string> GetSelectedWorkspacePathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Try to get selected item from the Workspace Explorer
            IVsMonitorSelection monitorSelection = await VS.Services.GetMonitorSelectionAsync();
            if (monitorSelection == null)
            {
                return null;
            }

            // Get the selection container
            if (ErrorHandler.Failed(monitorSelection.GetCurrentSelection(
                out IntPtr hierarchyPtr, out var itemId, out IVsMultiItemSelect multiSelect, out IntPtr containerPtr)))
            {
                return null;
            }

            try
            {
                // Try to get path from hierarchy
                if (hierarchyPtr != IntPtr.Zero)
                {
                    var hierarchy = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(hierarchyPtr);

                    // Try IVsHierarchy
                    if (hierarchy is IVsHierarchy vsHierarchy)
                    {
                        // Get canonical name (path) for the selected item
                        if (ErrorHandler.Succeeded(vsHierarchy.GetCanonicalName(itemId, out var canonicalName)) &&
                            !string.IsNullOrEmpty(canonicalName))
                        {
                            // Return directory path if it's a file, or the path itself if it's a directory
                            if (File.Exists(canonicalName))
                            {
                                return Path.GetDirectoryName(canonicalName);
                            }
                            else if (Directory.Exists(canonicalName))
                            {
                                return canonicalName;
                            }
                        }

                        // Try root path for the hierarchy (workspace root)
                        if (ErrorHandler.Succeeded(vsHierarchy.GetCanonicalName((uint)VSConstants.VSITEMID.Root, out var rootPath)) &&
                            !string.IsNullOrEmpty(rootPath) && Directory.Exists(rootPath))
                        {
                            return rootPath;
                        }
                    }
                }
            }
            finally
            {
                // Release COM objects
                if (hierarchyPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.Release(hierarchyPtr);
                }
                if (containerPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.Release(containerPtr);
                }
            }

            return null;
        }
    }
}
