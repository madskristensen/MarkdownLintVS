using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add space after heading hash (MD018).
    /// </summary>
    public class AddSpaceAfterHashAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add space after #";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var hashEnd = FindHashEnd(text);
            edit.Insert(line.Start + hashEnd, " ");
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var hashEnd = FindHashEnd(text);
            return text.Substring(0, hashEnd) + " " + text.Substring(hashEnd);
        }

        private static int FindHashEnd(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '#')
                    return i;
            }
            return text.Length;
        }
    }
}
