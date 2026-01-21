using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.CodeFixes.Actions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Fix action to fix all instances of a specific rule in the document.
    /// Reuses individual fix action classes via their ApplyFix method.
    /// </summary>
    public class FixAllInDocumentAction(ITextSnapshot snapshot, string ruleId, string filePath) : ISuggestedAction
    {
        public string DisplayText => $"Fix all {ruleId} violations in document";
        public string IconAutomationText => null;
        public ImageMoniker IconMoniker => default;
        public string InputGestureText => null;
        public bool HasActionSets => false;
        public bool HasPreview => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            var text = snapshot.GetText();
            var violations = MarkdownLintAnalyzer.Instance
                .Analyze(text, filePath)
                .Where(v => v.Rule.Id == ruleId)
                .OrderByDescending(v => v.LineNumber)
                .ThenByDescending(v => v.ColumnStart)
                .ToList();

            if (violations.Count == 0)
                return;

            using ITextEdit edit = snapshot.TextBuffer.CreateEdit();

            foreach (LintViolation violation in violations)
            {
                MarkdownFixAction action = MarkdownSuggestedActionsSource.CreateFixActionForViolation(violation, snapshot);
                action?.ApplyFix(edit);
            }

            edit.Apply();
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        void IDisposable.Dispose() { }
    }

    /// <summary>
    /// Fix action to fix all auto-fixable violations in the document.
    /// Reuses individual fix action classes via their ApplyFix method.
    /// </summary>
    public class FixAllAutoFixableAction(ITextSnapshot snapshot, string filePath) : ISuggestedAction
    {
        public string DisplayText => "Fix all auto-fixable violations in document";
        public string IconAutomationText => null;
        public ImageMoniker IconMoniker => default;
        public string InputGestureText => null;
        public bool HasActionSets => false;
        public bool HasPreview => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            var text = snapshot.GetText();
            var violations = MarkdownLintAnalyzer.Instance
                .Analyze(text, filePath)
                .Where(v => MarkdownSuggestedActionsSource.IsRuleAutoFixable(v.Rule.Id))
                .OrderByDescending(v => v.LineNumber)
                .ThenByDescending(v => v.ColumnStart)
                .ToList();

            if (violations.Count == 0)
                return;

            using ITextEdit edit = snapshot.TextBuffer.CreateEdit();

            // Track which lines already have a blank line being inserted BEFORE them.
            // This handles deduplication of:
            // - MD022 "add blank after heading" (inserts after line N = before line N+1)
            // - MD032 "add blank before list" (inserts before line M)
            // When list immediately follows heading, both try to insert before the same line.
            var blankLineBeforeLineNumbers = new HashSet<int>();

            foreach (LintViolation violation in violations)
            {
                MarkdownFixAction action = MarkdownSuggestedActionsSource.CreateFixActionForViolation(violation, snapshot);
                if (action == null)
                    continue;

                // Deduplicate blank line insertions that target the same line boundary
                if (action is AddBlankLineBeforeAction beforeAction)
                {
                    // "Add blank before line N" - the blank goes before line N
                    var targetLine = snapshot.GetLineFromPosition(beforeAction.InsertPosition).LineNumber;
                    if (blankLineBeforeLineNumbers.Contains(targetLine))
                        continue;

                    blankLineBeforeLineNumbers.Add(targetLine);
                }
                else if (action is AddBlankLineAfterAction afterAction)
                {
                    // "Add blank after line N" - the blank goes before line N+1
                    // InsertPosition is at EndIncludingLineBreak which is the start of next line
                    var targetLine = snapshot.GetLineFromPosition(afterAction.InsertPosition).LineNumber;
                    if (blankLineBeforeLineNumbers.Contains(targetLine))
                        continue;

                    blankLineBeforeLineNumbers.Add(targetLine);
                }
                else if (action is SurroundWithBlankLinesAction surroundAction)
                {
                    // SurroundWithBlankLinesAction may insert blanks before AND after the list
                    // Check each insertion point for duplicates
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

                    // If both would be skipped, skip the entire action
                    if (skipBefore && skipAfter)
                        continue;

                    // Apply with selective skipping
                    surroundAction.ApplyFix(edit, skipBefore, skipAfter);
                    continue;
                }

                action.ApplyFix(edit);
            }

            edit.Apply();
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        void IDisposable.Dispose() { }
    }
}
