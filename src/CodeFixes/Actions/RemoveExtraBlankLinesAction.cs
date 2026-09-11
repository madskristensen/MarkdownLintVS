using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove extra blank lines (MD012).
    /// </summary>
    [FixForRule("MD012")]
    public class RemoveExtraBlankLinesAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove extra blank lines";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine firstExtraLine = Snapshot.GetLineFromPosition(Span.Start);
            ITextSnapshotLine lastExtraLine = firstExtraLine;

            for (int lineNumber = firstExtraLine.LineNumber + 1; lineNumber < Snapshot.LineCount; lineNumber++)
            {
                ITextSnapshotLine line = Snapshot.GetLineFromLineNumber(lineNumber);
                if (!string.IsNullOrWhiteSpace(line.GetText()))
                    break;

                lastExtraLine = line;
            }

            edit.Delete(
                firstExtraLine.Start,
                lastExtraLine.EndIncludingLineBreak - firstExtraLine.Start);
        }

        protected override string GetFixedText()
        {
            return string.Empty;
        }
    }
}
