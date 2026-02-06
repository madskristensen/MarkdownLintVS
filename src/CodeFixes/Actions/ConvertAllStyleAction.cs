using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action that converts all instances of a style-consistency rule in the document
    /// to a single target style. Provides a more descriptive label than the generic
    /// "Fix all" action for style rules like MD004, MD049, and MD050.
    /// </summary>
    public class ConvertAllStyleAction(
        ITextSnapshot snapshot,
        string ruleId,
        string filePath,
        string targetStyle,
        string styleTypeName) : ISuggestedAction
    {
        /// <summary>
        /// Style-consistency rule IDs that support "Convert all" actions.
        /// </summary>
        private static readonly HashSet<string> _styleRuleIds =
        [
            "MD004", // Unordered list style
            "MD049", // Emphasis style
            "MD050", // Strong style
        ];

        /// <summary>
        /// Returns whether the given rule is a style-consistency rule that supports conversion.
        /// </summary>
        public static bool IsStyleRule(string ruleId) => _styleRuleIds.Contains(ruleId);

        /// <summary>
        /// Tries to create a convert-all action from a violation, or returns null
        /// if the rule is not a supported style rule or the target style cannot be determined.
        /// </summary>
        public static ConvertAllStyleAction TryCreate(
            LintViolation violation,
            ITextSnapshot snapshot,
            string filePath)
        {
            if (!IsStyleRule(violation.Rule.Id))
                return null;

            (var targetStyle, var styleTypeName) = GetStyleInfo(violation);
            if (targetStyle == null)
                return null;

            return new ConvertAllStyleAction(snapshot, violation.Rule.Id, filePath, targetStyle, styleTypeName);
        }

        private static (string TargetStyle, string StyleTypeName) GetStyleInfo(LintViolation violation)
        {
            switch (violation.Rule.Id)
            {
                case "MD004":
                    var marker = ViolationMessageParser.ExtractExpectedMarker(violation.Message);
                    if (marker.HasValue)
                    {
                        var markerName = marker.Value switch
                        {
                            '-' => "dash",
                            '*' => "asterisk",
                            '+' => "plus",
                            _ => marker.Value.ToString()
                        };
                        return (markerName, "list markers");
                    }
                    break;

                case "MD049":
                    var emphStyle = ViolationMessageParser.ExtractExpectedStyle(violation.Message);
                    if (emphStyle != null)
                        return (emphStyle, "emphasis");
                    break;

                case "MD050":
                    var strongStyle = ViolationMessageParser.ExtractExpectedStyle(violation.Message);
                    if (strongStyle != null)
                        return (strongStyle, "strong/bold");
                    break;
            }

            return (null, null);
        }

        public string DisplayText => $"Convert all {styleTypeName} to {targetStyle} style";
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
}
