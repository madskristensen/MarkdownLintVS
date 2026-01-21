using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add space before closing hash on closed atx heading (MD020).
    /// </summary>
    [FixForRule("MD020")]
    public class AddSpaceBeforeClosingHashAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add space before closing #";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();

            Match match = Regex.Match(text, @"^(#{1,6}\s+.+?)(\s*)(#{1,6})\s*$");
            if (match.Success)
            {
                var content = match.Groups[1].Value.TrimEnd();
                var closingHashes = match.Groups[3].Value;
                return content + " " + closingHashes;
            }
            return text;
        }
    }
}
