using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove spaces inside code span elements (MD038).
    /// </summary>
    public class RemoveSpaceInCodeSpanAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove spaces inside code span";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);
            var fixedText = RemoveSpacesInCodeSpan(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return RemoveSpacesInCodeSpan(Snapshot.GetText(Span));
        }

        private static string RemoveSpacesInCodeSpan(string text)
        {
            // Find backtick delimiters and trim content inside
            var backtickCount = 0;
            for (var i = 0; i < text.Length && text[i] == '`'; i++)
                backtickCount++;

            if (backtickCount > 0 && text.Length > backtickCount * 2)
            {
                var delimiter = new string('`', backtickCount);
                var content = text.Substring(backtickCount, text.Length - backtickCount * 2);
                return delimiter + content.Trim() + delimiter;
            }
            return text;
        }
    }
}
