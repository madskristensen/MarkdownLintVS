using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change horizontal rule style (MD035).
    /// </summary>
    public class ChangeHorizontalRuleStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _hrPattern = new(
            @"^(\s*)([-*_])(\s*\2\s*\2[\s\2]*)$",
            RegexOptions.Compiled);

        public override string DisplayText => $"Change horizontal rule to '{targetStyle}'";

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
