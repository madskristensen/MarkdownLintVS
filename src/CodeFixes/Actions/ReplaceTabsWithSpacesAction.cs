using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to replace all tabs with spaces on a line (MD010).
    /// </summary>
    public class ReplaceTabsWithSpacesAction(ITextSnapshot snapshot, Span span, int spacesPerTab = 4) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Replace tabs with spaces";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return line.GetText().Replace("\t", new string(' ', spacesPerTab));
        }
    }
}
