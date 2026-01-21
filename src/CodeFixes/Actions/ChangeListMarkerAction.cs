using System.Text.RegularExpressions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change unordered list marker style (MD004).
    /// </summary>
    [FixForRule("MD004", RequiresFactory = true)]
    public class ChangeListMarkerAction(ITextSnapshot snapshot, Span span, char targetMarker) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _listMarkerPattern = new(
            @"^(\s*)([-*+])(\s)",
            RegexOptions.Compiled);

        public override string DisplayText => $"Change list marker to '{targetMarker}'";

        /// <summary>
        /// Creates a fix action by extracting the expected marker from the violation message.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            var marker = ExtractExpectedMarker(violation.Message);
            return marker.HasValue ? new ChangeListMarkerAction(snapshot, span, marker.Value) : null;
        }

        private static char? ExtractExpectedMarker(string message)
        {
            if (message.Contains("expected 'dash'") || message.Contains("should use dash"))
                return '-';
            if (message.Contains("expected 'asterisk'") || message.Contains("should use asterisk"))
                return '*';
            if (message.Contains("expected 'plus'") || message.Contains("should use plus"))
                return '+';
            return null;
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return _listMarkerPattern.Replace(line.GetText(), $"$1{targetMarker}$3");
        }
    }
}
