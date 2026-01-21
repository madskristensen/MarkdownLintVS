using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add language to a fenced code block (MD040).
    /// </summary>
    [FixForRule("MD040")]
    public class AddCodeBlockLanguageAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add language identifier";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var fence = text.TrimStart();
            var indent = text.Length - fence.Length;
            var fenceChar = fence[0];
            var fenceLength = 0;

            for (var i = 0; i < fence.Length && fence[i] == fenceChar; i++)
                fenceLength++;

            return new string(' ', indent) + new string(fenceChar, fenceLength) + "text";
        }
    }
}
