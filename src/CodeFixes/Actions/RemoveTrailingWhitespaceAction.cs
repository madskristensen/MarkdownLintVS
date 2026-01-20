using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove trailing whitespace from a line (MD009).
    /// </summary>
    public class RemoveTrailingWhitespaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove trailing whitespace";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var lineText = line.GetText();
            var trimmedText = lineText.TrimEnd();

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, trimmedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return line.GetText().TrimEnd();
        }
    }
}
