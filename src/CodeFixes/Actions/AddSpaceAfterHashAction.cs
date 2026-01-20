using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add space after heading hash (MD018).
    /// </summary>
    public class AddSpaceAfterHashAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add space after #";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var hashEnd = text.LastIndexOf('#') + 1;

            // Find where hashes end
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '#')
                {
                    hashEnd = i;
                    break;
                }
            }

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(line.Start + hashEnd, " ");
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var hashEnd = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '#')
                {
                    hashEnd = i;
                    break;
                }
            }

            return text.Substring(0, hashEnd) + " " + text.Substring(hashEnd);
        }
    }
}
