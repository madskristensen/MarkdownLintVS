using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove trailing punctuation from a heading (MD026).
    /// </summary>
    public class RemoveTrailingPunctuationAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        private const string PunctuationChars = ".,;:!。，；：！";

        public override string DisplayText => "Remove trailing punctuation";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = RemovePunctuation(line.GetText());

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, text);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return RemovePunctuation(line.GetText());
        }

        private static string RemovePunctuation(string text)
        {
            text = text.TrimEnd();
            while (text.Length > 0 && PunctuationChars.Contains(text[text.Length - 1].ToString()))
            {
                text = text.Substring(0, text.Length - 1).TrimEnd();
            }
            return text;
        }
    }
}
