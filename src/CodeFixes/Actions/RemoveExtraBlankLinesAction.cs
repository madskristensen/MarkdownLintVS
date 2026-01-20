using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove extra blank lines (MD012).
    /// </summary>
    public class RemoveExtraBlankLinesAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove extra blank lines";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Delete(line.Start, line.LengthIncludingLineBreak);
        }

        protected override string GetFixedText()
        {
            return string.Empty;
        }
    }
}
