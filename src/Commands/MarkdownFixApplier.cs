using System.Collections.Generic;
using System.Linq;
using MarkdownLintVS.CodeFixes;
using MarkdownLintVS.CodeFixes.Actions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Provides shared functionality for applying markdown lint fixes to text buffers.
    /// Used by both Formatting commands and Save command handler.
    /// </summary>
    internal static class MarkdownFixApplier
    {
        /// <summary>
        /// Applies all auto-fixable markdown lint violations in the buffer.
        /// </summary>
        /// <param name="buffer">The text buffer to modify.</param>
        /// <param name="lineRange">Optional line range to restrict fixes to. If null, fixes entire document.</param>
        public static void ApplyAllFixes(ITextBuffer buffer, (int start, int end)? lineRange = null)
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
