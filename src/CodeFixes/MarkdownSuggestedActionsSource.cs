using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
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
    public class MarkdownSuggestedActionsSourceProvider : ISuggestedActionsSourceProvider
    {
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
        /// <summary>
        /// Context passed to fix action factories.
        /// </summary>
        private readonly struct FixContext(ITextSnapshot snapshot, ITextSnapshotLine line, LintViolation violation)
        {
            public ITextSnapshot Snapshot { get; } = snapshot;
            public ITextSnapshotLine Line { get; } = line;
            public LintViolation Violation { get; } = violation;
            public Span LineSpan => new(Line.Start, Line.Length);
            public Span ViolationSpan => new(Line.Start + Violation.ColumnStart, Violation.ColumnEnd - Violation.ColumnStart);
        }

        /// <summary>
        /// Registry of fix action factories keyed by rule ID.
        /// </summary>
        private static readonly Dictionary<string, Func<FixContext, ISuggestedAction>> _fixFactories = new()
        {
            ["MD004"] = ctx => ExtractExpectedMarker(ctx.Violation.Message) is char marker
                ? new ChangeListMarkerAction(ctx.Snapshot, ctx.LineSpan, marker) : null,
            ["MD009"] = ctx => new RemoveTrailingWhitespaceAction(ctx.Snapshot, ctx.LineSpan),
            ["MD010"] = ctx => new ReplaceTabsWithSpacesAction(ctx.Snapshot, ctx.LineSpan),
            ["MD011"] = ctx => new FixReversedLinkAction(ctx.Snapshot, ctx.ViolationSpan),
            ["MD012"] = ctx => new RemoveExtraBlankLinesAction(ctx.Snapshot, ctx.LineSpan),
            ["MD014"] = ctx => new RemoveDollarSignAction(ctx.Snapshot, ctx.LineSpan),
            ["MD018"] = ctx => new AddSpaceAfterHashAction(ctx.Snapshot, ctx.LineSpan),
            ["MD019"] = ctx => new NormalizeWhitespaceAction(ctx.Snapshot, ctx.LineSpan),
            ["MD020"] = ctx => new AddSpaceBeforeClosingHashAction(ctx.Snapshot, ctx.LineSpan),
            ["MD021"] = ctx => new NormalizeWhitespaceAction(ctx.Snapshot, ctx.LineSpan),
            ["MD022"] = ctx => CreateBlankLineAction(ctx),
            ["MD023"] = ctx => new RemoveLeadingWhitespaceAction(ctx.Snapshot, ctx.LineSpan),
            ["MD026"] = ctx => new RemoveTrailingPunctuationAction(ctx.Snapshot, ctx.LineSpan),
            ["MD027"] = ctx => new NormalizeWhitespaceAction(ctx.Snapshot, ctx.LineSpan),
            ["MD028"] = ctx => new AddBlockquotePrefixAction(ctx.Snapshot, ctx.LineSpan),
            ["MD029"] = ctx => ExtractExpectedNumber(ctx.Violation.Message) is int num
                ? new FixOrderedListPrefixAction(ctx.Snapshot, ctx.LineSpan, num) : null,
            ["MD030"] = ctx => new NormalizeListMarkerSpaceAction(ctx.Snapshot, ctx.LineSpan),
            ["MD031"] = ctx => CreateBlankLineAction(ctx),
            ["MD032"] = ctx => new SurroundWithBlankLinesAction(ctx.Snapshot, ctx.LineSpan),
            ["MD034"] = ctx => new WrapUrlInBracketsAction(ctx.Snapshot, ctx.ViolationSpan),
            ["MD035"] = ctx => ExtractExpectedStyle(ctx.Violation.Message) is string style
                ? new ChangeHorizontalRuleStyleAction(ctx.Snapshot, ctx.LineSpan, style) : null,
            ["MD037"] = ctx => new RemoveSpaceInEmphasisAction(ctx.Snapshot, ctx.ViolationSpan),
            ["MD038"] = ctx => new RemoveSpaceInCodeSpanAction(ctx.Snapshot, ctx.ViolationSpan),
            ["MD039"] = ctx => new RemoveSpaceInLinkTextAction(ctx.Snapshot, ctx.ViolationSpan),
            ["MD040"] = ctx => new AddCodeBlockLanguageAction(ctx.Snapshot, ctx.LineSpan),
            ["MD045"] = ctx => new AddImageAltTextAction(ctx.Snapshot, ctx.ViolationSpan),
            ["MD047"] = ctx => ctx.Violation.Message.Contains("multiple")
                ? new RemoveExtraBlankLinesAction(ctx.Snapshot, new Span(ctx.Snapshot.Length - 1, 1))
                : new AddFinalNewlineAction(ctx.Snapshot, new Span(ctx.Snapshot.Length, 0)),
            ["MD048"] = ctx => ExtractExpectedStyle(ctx.Violation.Message) is string style
                ? new ChangeCodeFenceStyleAction(ctx.Snapshot, ctx.LineSpan, style) : null,
            ["MD049"] = ctx => ExtractExpectedStyle(ctx.Violation.Message) is string style
                ? new ChangeEmphasisStyleAction(ctx.Snapshot, ctx.ViolationSpan, style) : null,
            ["MD050"] = ctx => ExtractExpectedStyle(ctx.Violation.Message) is string style
                ? new ChangeStrongStyleAction(ctx.Snapshot, ctx.ViolationSpan, style) : null,
            ["MD058"] = ctx => CreateBlankLineAction(ctx),
        };

        private static MarkdownFixAction CreateBlankLineAction(FixContext ctx)
        {
            var fixDesc = ctx.Violation.FixDescription ?? ctx.Violation.Message;
            if (fixDesc.Contains("before"))
                return new AddBlankLineBeforeAction(ctx.Snapshot, ctx.LineSpan);
            if (fixDesc.Contains("after"))
                return new AddBlankLineAfterAction(ctx.Snapshot, ctx.LineSpan);
            return null;
        }

        /// <summary>
        /// Creates a fix action for a violation. Used by both single-fix and fix-all operations.
        /// </summary>
        public static MarkdownFixAction CreateFixActionForViolation(LintViolation violation, ITextSnapshot snapshot)
        {
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(Math.Min(violation.LineNumber, snapshot.LineCount - 1));
            var context = new FixContext(snapshot, line, violation);

            if (_fixFactories.TryGetValue(violation.Rule.Id, out Func<FixContext, ISuggestedAction> factory))
            {
                return factory(context) as MarkdownFixAction;
            }

            return null;
        }

        /// <summary>
        /// Returns whether a rule has an auto-fix available.
        /// </summary>
        public static bool IsRuleAutoFixable(string ruleId) => _fixFactories.ContainsKey(ruleId);

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

        private static char? ExtractExpectedMarker(string message)
        {
            // Extract marker from messages like "expected 'dash'" or "should use dash"
            // Must look for "expected 'X'" pattern to avoid matching "found 'Y'" part
            if (message.Contains("expected 'dash'") || message.Contains("should use dash"))
                return '-';
            if (message.Contains("expected 'asterisk'") || message.Contains("should use asterisk"))
                return '*';
            if (message.Contains("expected 'plus'") || message.Contains("should use plus"))
                return '+';
            return null;
        }

        private static int? ExtractExpectedNumber(string message)
        {
            // Extract number from messages like "should be '1'" or "should be '2'"
            Match match = System.Text.RegularExpressions.Regex.Match(message, @"should be '(\d+)'");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
                return number;
            return null;
        }

        private static string ExtractExpectedStyle(string message)
        {
            // Extract style from messages like "expected backtick)", "expected asterisk)"
            // Look for "expected X" pattern to avoid matching "found Y" part
            if (message.Contains("expected backtick")) return "backtick";
            if (message.Contains("expected tilde")) return "tilde";
            if (message.Contains("expected asterisk")) return "asterisk";
            if (message.Contains("expected underscore")) return "underscore";
            // For "should be X" patterns
            if (message.Contains("should be backtick")) return "backtick";
            if (message.Contains("should be tilde")) return "tilde";
            if (message.Contains("should be asterisk")) return "asterisk";
            if (message.Contains("should be underscore")) return "underscore";
            // For horizontal rules
            if (message.Contains("---")) return "---";
            if (message.Contains("***")) return "***";
            if (message.Contains("___")) return "___";
            return null;
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
