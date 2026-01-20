using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove extra blank lines (MD012).
    /// </summary>
    public class RemoveExtraBlankLinesAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove extra blank lines";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                // Delete the entire blank line including line break
                edit.Delete(line.Start, line.LengthIncludingLineBreak);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return string.Empty;
        }
    }
}
