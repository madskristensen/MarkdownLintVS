using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.CodeFixes.Actions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Provider for markdown lint suggested actions.
    /// </summary>
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Markdown Lint Suggested Actions")]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    public class MarkdownSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import]
        internal MarkdownAnalysisCache AnalysisCache { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null || textView == null)
                return null;

            var filePath = GetFilePath(textBuffer);
            return new MarkdownSuggestedActionsSource(textBuffer, AnalysisCache, filePath);
        }

        private string GetFilePath(ITextBuffer buffer)
        {
            if (buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
            {
                return document.FilePath;
            }
            return null;
        }
    }

    /// <summary>
    /// Source for markdown lint suggested actions.
    /// Uses FixActionRegistry to auto-discover available fix actions.
    /// </summary>
    internal class MarkdownSuggestedActionsSource(ITextBuffer buffer, MarkdownAnalysisCache analysisCache, string filePath) : ISuggestedActionsSource2
    {
        /// <summary>
        /// Creates a fix action for a violation. Used by both single-fix and fix-all operations.
        /// </summary>
        public static MarkdownFixAction CreateFixActionForViolation(LintViolation violation, ITextSnapshot snapshot)
        {
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(Math.Min(violation.LineNumber, snapshot.LineCount - 1));
            return FixActionRegistry.CreateFix(violation, snapshot, line);
        }

        /// <summary>
        /// Returns whether a rule has an auto-fix available.
        /// </summary>
        public static bool IsRuleAutoFixable(string ruleId) => FixActionRegistry.HasFix(ruleId);

        public event EventHandler<EventArgs> SuggestedActionsChanged { add { } remove { } }

        public Task<bool> HasSuggestedActionsAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                IEnumerable<LintViolation> violations = GetViolationsAtRange(range);
                return violations.Any();
            }, cancellationToken);
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            return GetSuggestedActionsImpl(range);
        }

        public async Task<ISuggestedActionCategorySet> GetSuggestedActionCategoriesAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            CancellationToken cancellationToken)
        {
            var hasActions = await HasSuggestedActionsAsync(requestedActionCategories, range, cancellationToken);
            return hasActions ? requestedActionCategories : null;
        }

        public async IAsyncEnumerable<SuggestedActionSet> GetSuggestedActionsAsync(
            ISuggestedActionCategorySet requestedActionCategories,
            SnapshotSpan range,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            List<SuggestedActionSet> actionSets = await Task.Run(() => GetSuggestedActionsImpl(range).ToList(), cancellationToken);

            foreach (SuggestedActionSet actionSet in actionSets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return actionSet;
            }
        }

        private IEnumerable<SuggestedActionSet> GetSuggestedActionsImpl(SnapshotSpan range)
        {
            var violations = GetViolationsAtRange(range).ToList();
            var actions = new List<ISuggestedAction>();
            var fixAllActions = new List<ISuggestedAction>();
            var seenRules = new HashSet<string>();
            var seenRuleLines = new HashSet<(string RuleId, int LineNumber)>();

            foreach (LintViolation violation in violations)
            {
                // Deduplicate actions by rule and line (e.g., multiple tabs on same line)
                (string Id, int LineNumber) ruleLineKey = (violation.Rule.Id, violation.LineNumber);
                if (seenRuleLines.Contains(ruleLineKey))
                    continue;
                seenRuleLines.Add(ruleLineKey);

                ISuggestedAction action = CreateFixActionForViolation(violation, range.Snapshot);
                if (action != null)
                {
                    actions.Add(action);

                    // Add "Fix all" action for this rule type (once per rule)
                    if (!seenRules.Contains(violation.Rule.Id) && IsRuleAutoFixable(violation.Rule.Id))
                    {
                        seenRules.Add(violation.Rule.Id);
                        fixAllActions.Add(new FixAllInDocumentAction(range.Snapshot, violation.Rule.Id, filePath));

                        // For style-consistency rules, add a more descriptive "Convert all" action
                        var convertAction = ConvertAllStyleAction.TryCreate(violation, range.Snapshot, filePath);
                        if (convertAction != null)
                        {
                            fixAllActions.Add(convertAction);
                        }
                    }
                }
            }

            if (actions.Count > 0)
            {
                yield return new SuggestedActionSet(
                    categoryName: PredefinedSuggestedActionCategoryNames.CodeFix,
                    actions: actions,
                    title: "Markdown Lint Fixes",
                    priority: SuggestedActionSetPriority.Medium);
            }

            if (fixAllActions.Count > 0)
            {
                // Add "Fix all auto-fixable" action
                fixAllActions.Add(new FixAllAutoFixableAction(range.Snapshot, filePath));

                yield return new SuggestedActionSet(
                    categoryName: PredefinedSuggestedActionCategoryNames.CodeFix,
                    actions: fixAllActions,
                    title: "Fix All",
                    priority: SuggestedActionSetPriority.Low);
            }
        }

        private IEnumerable<LintViolation> GetViolationsAtRange(SnapshotSpan range)
        {
            IReadOnlyList<LintViolation> violations = analysisCache.GetOrAnalyze(buffer, filePath);

            foreach (LintViolation violation in violations)
            {
                if (violation.Severity == DiagnosticSeverity.None ||
                    violation.Severity == DiagnosticSeverity.Silent)
                    continue;

                var lineNumber = violation.LineNumber;
                if (lineNumber < 0 || lineNumber >= range.Snapshot.LineCount)
                    continue;

                ITextSnapshotLine line = range.Snapshot.GetLineFromLineNumber(lineNumber);

                // Clamp column values to valid range within the line
                var columnStart = Math.Max(0, Math.Min(violation.ColumnStart, line.Length));
                var columnEnd = Math.Max(columnStart, Math.Min(violation.ColumnEnd, line.Length));
                var spanLength = Math.Max(1, columnEnd - columnStart);

                // Ensure the span doesn't exceed the snapshot length
                int start = line.Start + columnStart;
                if (start >= range.Snapshot.Length)
                    continue;

                spanLength = Math.Min(spanLength, range.Snapshot.Length - start);
                if (spanLength <= 0)
                    continue;

                var violationSpan = new SnapshotSpan(range.Snapshot, new Span(start, spanLength));

                if (violationSpan.IntersectsWith(range))
                {
                    yield return violation;
                }
            }
        }

        public void Dispose()
        {
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }
    }
}
