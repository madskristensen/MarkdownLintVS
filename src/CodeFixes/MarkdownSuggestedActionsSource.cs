using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Provider for markdown lint suggested actions.
    /// </summary>
    [Export(typeof(ISuggestedActionsSourceProvider))]
    [Name("Markdown Lint Suggested Actions")]
    [ContentType("markdown")]
    public class MarkdownSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
        [Import(typeof(ITextStructureNavigatorSelectorService))]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        public ISuggestedActionsSource CreateSuggestedActionsSource(ITextView textView, ITextBuffer textBuffer)
        {
            if (textBuffer == null || textView == null)
                return null;

            var filePath = GetFilePath(textBuffer);
            return new MarkdownSuggestedActionsSource(filePath);
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
    /// </summary>
    internal class MarkdownSuggestedActionsSource(string filePath) : ISuggestedActionsSource2
    {
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

            foreach (LintViolation violation in violations)
            {
                ISuggestedAction action = CreateFixAction(violation, range.Snapshot);
                if (action != null)
                {
                    actions.Add(action);

                    // Add "Fix all" action for this rule type (once per rule)
                    if (!seenRules.Contains(violation.Rule.Id) && IsAutoFixable(violation.Rule.Id))
                    {
                        seenRules.Add(violation.Rule.Id);
                        fixAllActions.Add(new FixAllInDocumentAction(range.Snapshot, violation.Rule.Id, filePath));
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

        private static readonly HashSet<string> _autoFixableRuleIds =
        [
            "MD009", "MD010", "MD012", "MD018", "MD019", "MD022", "MD023", "MD027",
            "MD031", "MD032", "MD058"
        ];

        private bool IsAutoFixable(string ruleId)
        {
            return _autoFixableRuleIds.Contains(ruleId);
        }

        private IEnumerable<LintViolation> GetViolationsAtRange(SnapshotSpan range)
        {
            var text = range.Snapshot.GetText();
            IEnumerable<LintViolation> violations = MarkdownLintAnalyzer.Instance.Analyze(text, filePath);

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

        private ISuggestedAction CreateFixAction(LintViolation violation, ITextSnapshot snapshot)
        {
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(Math.Min(violation.LineNumber, snapshot.LineCount - 1));
            var span = new Span(line.Start, line.Length);

            switch (violation.Rule.Id)
            {
                case "MD009": // Trailing spaces
                    return new RemoveTrailingWhitespaceAction(snapshot, span);

                case "MD010": // Hard tabs
                    var tabSpan = new Span(line.Start + violation.ColumnStart, 1);
                    return new ReplaceTabsWithSpacesAction(snapshot, tabSpan);

                case "MD011": // Reversed links
                    var linkSpan = new Span(line.Start + violation.ColumnStart, violation.ColumnEnd - violation.ColumnStart);
                    return new FixReversedLinkAction(snapshot, linkSpan);

                case "MD012": // Multiple blank lines
                    return new RemoveExtraBlankLinesAction(snapshot, span);

                case "MD018": // No space after hash
                    return new AddSpaceAfterHashAction(snapshot, span);

                case "MD019": // Multiple spaces after hash
                case "MD021": // Multiple spaces in closed atx
                case "MD027": // Multiple spaces after blockquote
                    return new NormalizeWhitespaceAction(snapshot, span);

                case "MD022": // Blanks around headings
                case "MD031": // Blanks around fences
                case "MD032": // Blanks around lists
                case "MD058": // Blanks around tables
                    if (violation.Message.Contains("preceded") || violation.Message.Contains("before"))
                        return new AddBlankLineBeforeAction(snapshot, span);
                    else
                        return new AddBlankLineAfterAction(snapshot, span);

                case "MD023": // Heading start left
                    return new RemoveLeadingWhitespaceAction(snapshot, span);

                case "MD026": // Trailing punctuation
                    return new RemoveTrailingPunctuationAction(snapshot, span);

                case "MD034": // Bare URLs
                    var urlSpan = new Span(line.Start + violation.ColumnStart, violation.ColumnEnd - violation.ColumnStart);
                    return new WrapUrlInBracketsAction(snapshot, urlSpan);

                case "MD040": // Fenced code language
                    return new AddCodeBlockLanguageAction(snapshot, span);

                case "MD045": // No alt text
                    var imgSpan = new Span(line.Start + violation.ColumnStart, violation.ColumnEnd - violation.ColumnStart);
                    return new AddImageAltTextAction(snapshot, imgSpan);

                case "MD047": // Single trailing newline
                    if (violation.Message.Contains("multiple"))
                        return new RemoveExtraBlankLinesAction(snapshot, new Span(snapshot.Length - 1, 1));
                    else
                        return new AddFinalNewlineAction(snapshot, new Span(snapshot.Length, 0));

                default:
                    return null;
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
