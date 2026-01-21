using System.Text.RegularExpressions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to fix ordered list item prefix (MD029).
    /// Changes the number prefix to match the expected style.
    /// </summary>
    [FixForRule("MD029", RequiresFactory = true)]
    public class FixOrderedListPrefixAction(ITextSnapshot snapshot, Span span, int expectedNumber) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _orderedListPattern = new(
            @"^(\s*)(\d+)(\.)\s",
            RegexOptions.Compiled);

        public override string DisplayText => $"Change list prefix to '{expectedNumber}.'";

        /// <summary>
        /// Creates a fix action by extracting the expected number from the violation message.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            var number = ViolationMessageParser.ExtractExpectedNumber(violation.Message);
            return number.HasValue ? new FixOrderedListPrefixAction(snapshot, span, number.Value) : null;
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return _orderedListPattern.Replace(line.GetText(), $"$1{expectedNumber}$3 ");
        }
    }
}
