using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to normalize multiple spaces to single space (MD019, MD021, MD027).
    /// </summary>
    [FixForRule("MD019")]
    [FixForRule("MD021")]
    [FixForRule("MD027")]
    public class NormalizeWhitespaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Use single space";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            return Regex.Replace(text, @"(\S)  +", "$1 ");
        }
    }
}
