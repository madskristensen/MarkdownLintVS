using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change strong/bold style between asterisk and underscore (MD050).
    /// </summary>
    public class ChangeStrongStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => $"Change strong to {targetStyle}";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);
            var fixedText = ChangeStrongStyle(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return ChangeStrongStyle(Snapshot.GetText(Span));
        }

        private string ChangeStrongStyle(string text)
        {
            // Match double emphasis: **text** or __text__
            if (targetStyle == "asterisk")
            {
                // Change __text__ to **text**
                return Regex.Replace(text, @"__([^_]+)__", "**$1**");
            }
            else
            {
                // Change **text** to __text__
                return Regex.Replace(text, @"\*\*([^*]+)\*\*", "__$1__");
            }
        }
    }
}
