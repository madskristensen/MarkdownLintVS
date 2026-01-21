using System.Collections.Generic;
using System.Linq;
using MarkdownLintVS.CodeFixes;
using MarkdownLintVS.CodeFixes.Actions;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio;
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
        private const string MarkdownContentType = "markdown";
        private const string VsMarkdownContentType = "vs-markdown";

        public static async Task InitializeAsync()
        {
            // Intercept the formatting commands for Markdown files
            await VS.Commands.InterceptAsync(VSConstants.VSStd2KCmdID.FORMATDOCUMENT, () => ExecuteOnMarkdownDocument(FormatDocument));
            await VS.Commands.InterceptAsync(VSConstants.VSStd2KCmdID.FORMATSELECTION, () => ExecuteOnMarkdownDocument(FormatSelection));
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

                    // If Off or user cancelled, let Visual Studio handle the command normally
                    return behavior == FormatDocumentBehavior.On ? CommandProgression.Stop : CommandProgression.Continue;
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
            return contentType.IsOfType(MarkdownContentType) || contentType.IsOfType(VsMarkdownContentType);
        }

        private static void FormatDocument(DocumentView doc)
        {
            ApplyAllFixes(doc.TextBuffer, null);
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

            ApplyAllFixes(doc.TextBuffer, (startLine, endLine));
        }

        /// <summary>
        /// Applies all auto-fixable markdown lint violations in the buffer.
        /// </summary>
        /// <param name="buffer">The text buffer to modify.</param>
        /// <param name="lineRange">Optional line range to restrict fixes to. If null, fixes entire document.</param>
        private static void ApplyAllFixes(ITextBuffer buffer, (int start, int end)? lineRange)
        {
            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            var text = snapshot.GetText();

            // Get file path for analysis
            string filePath = null;
            if (buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
            {
                filePath = document.FilePath;
            }

            // Get all auto-fixable violations
            var violations = MarkdownLintAnalyzer.Instance
                .Analyze(text, filePath)
                .Where(v => MarkdownSuggestedActionsSource.IsRuleAutoFixable(v.Rule.Id))
                .Where(v => lineRange == null || (v.LineNumber >= lineRange.Value.start && v.LineNumber <= lineRange.Value.end))
                .OrderByDescending(v => v.LineNumber)
                .ThenByDescending(v => v.ColumnStart)
                .ToList();

            if (violations.Count == 0)
                return;

            using ITextEdit edit = buffer.CreateEdit();

            // Track which lines already have a blank line being inserted BEFORE them.
            var blankLineBeforeLineNumbers = new HashSet<int>();

            foreach (LintViolation violation in violations)
            {
                MarkdownFixAction action = MarkdownSuggestedActionsSource.CreateFixActionForViolation(violation, snapshot);
                if (action == null)
                    continue;

                // Deduplicate blank line insertions that target the same line boundary
                if (action is AddBlankLineBeforeAction beforeAction)
                {
                    var targetLine = snapshot.GetLineFromPosition(beforeAction.InsertPosition).LineNumber;
                    if (blankLineBeforeLineNumbers.Contains(targetLine))
                        continue;

                    blankLineBeforeLineNumbers.Add(targetLine);
                }
                else if (action is AddBlankLineAfterAction afterAction)
                {
                    var targetLine = snapshot.GetLineFromPosition(afterAction.InsertPosition).LineNumber;
                    if (blankLineBeforeLineNumbers.Contains(targetLine))
                        continue;

                    blankLineBeforeLineNumbers.Add(targetLine);
                }
                else if (action is SurroundWithBlankLinesAction surroundAction)
                {
                    var skipBefore = false;
                    var skipAfter = false;

                    var beforeLine = surroundAction.InsertBeforeLine;
                    if (beforeLine >= 0)
                    {
                        if (blankLineBeforeLineNumbers.Contains(beforeLine))
                            skipBefore = true;
                        else
                            blankLineBeforeLineNumbers.Add(beforeLine);
                    }

                    var afterLine = surroundAction.InsertAfterListBeforeLine;
                    if (afterLine >= 0)
                    {
                        if (blankLineBeforeLineNumbers.Contains(afterLine))
                            skipAfter = true;
                        else
                            blankLineBeforeLineNumbers.Add(afterLine);
                    }

                    if (skipBefore && skipAfter)
                        continue;

                    surroundAction.ApplyFix(edit, skipBefore, skipAfter);
                    continue;
                }

                action.ApplyFix(edit);
            }

            edit.Apply();
        }
    }
}
