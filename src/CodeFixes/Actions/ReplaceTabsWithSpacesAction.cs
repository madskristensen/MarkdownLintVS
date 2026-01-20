using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to replace all tabs with spaces on a line (MD010).
    /// </summary>
    public class ReplaceTabsWithSpacesAction(ITextSnapshot snapshot, Span span, int spacesPerTab = 4) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Replace tabs with spaces";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var lineText = line.GetText();
            var fixedText = lineText.Replace("\t", new string(' ', spacesPerTab));

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var lineText = line.GetText();
            return lineText.Replace("\t", new string(' ', spacesPerTab));
        }
    }
}
