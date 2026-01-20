using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add language to a fenced code block (MD040).
    /// </summary>
    public class AddCodeBlockLanguageAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add language identifier";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var fence = text.TrimStart();
            var indent = text.Length - fence.Length;
            var fenceChar = fence[0];
            var fenceLength = 0;

            for (var i = 0; i < fence.Length && fence[i] == fenceChar; i++)
                fenceLength++;

            var newText = new string(' ', indent) + new string(fenceChar, fenceLength) + "text";

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, newText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            return text.TrimEnd() + "text";
        }
    }
}
