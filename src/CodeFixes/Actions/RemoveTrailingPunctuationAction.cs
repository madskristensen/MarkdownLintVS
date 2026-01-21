using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove trailing punctuation from a heading (MD026).
    /// </summary>
    [FixForRule("MD026")]
    public class RemoveTrailingPunctuationAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        private const string PunctuationChars = ".,;:!。，；：！";

        public override string DisplayText => "Remove trailing punctuation";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText().TrimEnd();
            while (text.Length > 0 && PunctuationChars.Contains(text[text.Length - 1].ToString()))
            {
                text = text.Substring(0, text.Length - 1).TrimEnd();
            }
            return text;
        }
    }
}
