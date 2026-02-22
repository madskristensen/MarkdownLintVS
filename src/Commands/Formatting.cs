using System.ComponentModel.Composition;
using System.Linq;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Handles Format Document and Format Selection commands for Markdown files.
    /// Applies all auto-fixable markdown lint violations.
    /// </summary>
    public static class Formatting
    {
        private static MarkdownAnalysisCache _analysisCache;

        public static async Task InitializeAsync()
        {
            // Get the analysis cache from MEF
            var componentModel = await VS.GetServiceAsync<SComponentModel, IComponentModel>();
            _analysisCache = componentModel?.GetService<MarkdownAnalysisCache>();

            // Intercept the formatting commands for Markdown files
            _ = await VS.Commands.InterceptAsync(VSConstants.VSStd2KCmdID.FORMATDOCUMENT, () => ExecuteOnMarkdownDocument(FormatDocument));
            _ = await VS.Commands.InterceptAsync(VSConstants.VSStd2KCmdID.FORMATSELECTION, () => ExecuteOnMarkdownDocument(FormatSelection));
        }

        /// <summary>
        /// Executes an action on the active Markdown document.
        /// Returns Stop if the action was executed, Continue otherwise.
        /// </summary>
        private static CommandProgression ExecuteOnMarkdownDocument(Action<DocumentView> action)
        {
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                DocumentView doc = await VS.Documents.GetActiveDocumentViewAsync();

                if (doc?.TextBuffer != null && IsMarkdownContentType(doc.TextBuffer.ContentType))
                {
                    // Check user preference
                    FormatDocumentBehavior behavior = GeneralOptions.Instance.FormatDocumentBehavior;

                    if (behavior == FormatDocumentBehavior.Ask)
                    {
                        behavior = await PromptUserForBehaviorAsync();
                    }

                    if (behavior == FormatDocumentBehavior.On)
                    {
                        action(doc);
                    }
                }

                return CommandProgression.Continue;
            });
        }

        /// <summary>
        /// Prompts the user to choose their preferred formatting behavior.
        /// </summary>
        private static async System.Threading.Tasks.Task<FormatDocumentBehavior> PromptUserForBehaviorAsync()
        {
            // Use YESNOCANCEL: Yes = Enable, No = Disable, Cancel = Skip
            VSConstants.MessageBoxResult result = await VS.MessageBox.ShowAsync(
                "Markdown Lint - Format Document",
                "Would you like Markdown Lint to automatically fix violations when using Format Document/Selection?\n\n" +
                "Click 'Yes' to enable auto-fixing (recommended).\n" +
                "Click 'No' to disable auto-fixing.\n" +
                "Click 'Cancel' to skip this time without saving preference.\n\n" +
                "You can change this setting later in Tools > Options > Markdown Lint > General.",
                OLEMSGICON.OLEMSGICON_QUERY,
                OLEMSGBUTTON.OLEMSGBUTTON_YESNOCANCEL);

            switch (result)
            {
                case VSConstants.MessageBoxResult.IDYES:
                    GeneralOptions.Instance.FormatDocumentBehavior = FormatDocumentBehavior.On;
                    await GeneralOptions.Instance.SaveAsync();
                    return FormatDocumentBehavior.On;

                case VSConstants.MessageBoxResult.IDNO:
                    GeneralOptions.Instance.FormatDocumentBehavior = FormatDocumentBehavior.Off;
                    await GeneralOptions.Instance.SaveAsync();
                    return FormatDocumentBehavior.Off;

                default:
                    // Cancel - don't save, just skip this time
                    return FormatDocumentBehavior.Off;
            }
        }

        private static bool IsMarkdownContentType(Microsoft.VisualStudio.Utilities.IContentType contentType)
        {
            return contentType.IsOfType(ContentTypes.Markdown) || contentType.IsOfType(ContentTypes.VsMarkdown);
        }

        private static void FormatDocument(DocumentView doc)
        {
            MarkdownFixApplier.ApplyAllFixes(doc.TextBuffer, _analysisCache);
        }

        private static void FormatSelection(DocumentView doc)
        {
            SnapshotSpan selection = doc.TextView.Selection.SelectedSpans.FirstOrDefault();

            if (selection.IsEmpty)
            {
                // If no selection, format the entire document
                FormatDocument(doc);
                return;
            }

            // Get line numbers for the selection
            ITextSnapshot snapshot = doc.TextBuffer.CurrentSnapshot;
            var startLine = snapshot.GetLineNumberFromPosition(selection.Start);
            var endLine = snapshot.GetLineNumberFromPosition(selection.End);

            MarkdownFixApplier.ApplyAllFixes(doc.TextBuffer, _analysisCache, (startLine, endLine));
        }
    }
}
