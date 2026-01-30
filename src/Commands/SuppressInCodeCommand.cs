using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Command to suppress a markdownlint error by inserting a disable-line comment in the source code.
    /// Appears in the Error List context menu for markdownlint errors (error codes starting with "MD").
    /// </summary>
    [Command(PackageIds.SuppressInCodeCommand)]
    internal sealed class SuppressInCodeCommand : BaseCommand<SuppressInCodeCommand>
    {
        private string _errorCode;
        private string _filePath;
        private int _line;

        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Visible = false;
            _errorCode = null;
            _filePath = null;
            _line = 0;

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                // Get the Error List tool window
                IVsUIShell uiShell = await VS.Services.GetUIShellAsync();
                var errorListGuid = new Guid(ToolWindowGuids80.ErrorList);
                var hr = uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fFrameOnly, ref errorListGuid, out IVsWindowFrame frame);
                if (hr != 0 || frame == null)
                    return;

                // Get the IErrorList interface from the window frame
                frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out var docView);
                if (docView is not IErrorList errorList)
                    return;

                IWpfTableControl tableControl = errorList.TableControl;
                if (tableControl == null)
                    return;

                // Get selected entries
                IEnumerable<ITableEntryHandle> selectedEntries = tableControl.SelectedEntries;
                if (selectedEntries == null || !selectedEntries.Any())
                    return;

                // Get the first selected entry
                ITableEntryHandle entry = selectedEntries.First();

                // Check if it's a markdownlint error (error code starts with "MD")
                if (!entry.TryGetValue(StandardTableKeyNames.ErrorCode, out string errorCode) ||
                    string.IsNullOrEmpty(errorCode) ||
                    !errorCode.StartsWith("MD", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Get file path and line number
                if (!entry.TryGetValue(StandardTableKeyNames.DocumentName, out string filePath) ||
                    string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                if (!entry.TryGetValue(StandardTableKeyNames.Line, out int line))
                {
                    return;
                }

                // Store values for execute
                _errorCode = errorCode;
                _filePath = filePath;
                _line = line;

                Command.Visible = true;
            });
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (string.IsNullOrEmpty(_errorCode) || string.IsNullOrEmpty(_filePath))
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Open the document
            DocumentView docView = await VS.Documents.OpenAsync(_filePath);
            if (docView?.TextBuffer == null)
                return;

            SuppressionHelper.SuppressOnLine(docView.TextBuffer, _line, _errorCode);
        }
    }
}
