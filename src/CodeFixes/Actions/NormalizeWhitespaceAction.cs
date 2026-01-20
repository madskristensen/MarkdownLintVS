using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to normalize multiple spaces to single space (MD019, MD021, MD027).
    /// </summary>
    public class NormalizeWhitespaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Use single space";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var normalized = Regex.Replace(text, @"(\S)  +", "$1 ");

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, normalized);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            return Regex.Replace(text, @"(\S)  +", "$1 ");
        }
    }
}
