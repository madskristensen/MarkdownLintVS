using System.Text.RegularExpressions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change horizontal rule style (MD035).
    /// </summary>
    [FixForRule("MD035", RequiresFactory = true)]
    public class ChangeHorizontalRuleStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _hrPattern = new(
            @"^(\s*)([-*_])(\s*\2\s*\2[\s\2]*)$",
            RegexOptions.Compiled);

        public override string DisplayText => $"Change horizontal rule to '{targetStyle}'";

        /// <summary>
        /// Creates a fix action by extracting the expected style from the violation message.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            var style = ViolationMessageParser.ExtractExpectedStyle(violation.Message);
            return style != null ? new ChangeHorizontalRuleStyleAction(snapshot, span, style) : null;
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();

            Match match = _hrPattern.Match(text);
            if (match.Success)
            {
                var leadingWhitespace = match.Groups[1].Value;
                return leadingWhitespace + targetStyle;
            }
            return targetStyle;
        }
    }
}
