using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change emphasis style between asterisk and underscore (MD049).
    /// </summary>
    public class ChangeEmphasisStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => $"Change emphasis to {targetStyle}";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);
            var fixedText = ChangeEmphasisStyle(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return ChangeEmphasisStyle(Snapshot.GetText(Span));
        }

        private string ChangeEmphasisStyle(string text)
        {
            // Match single emphasis: *text* or _text_
            if (targetStyle == "asterisk")
            {
                // Change _text_ to *text*
                return Regex.Replace(text, @"(?<!\*)_([^_*]+)_(?!\*)", "*$1*");
            }
            else
            {
                // Change *text* to _text_
                return Regex.Replace(text, @"(?<!\*)\*([^_*]+)\*(?!\*)", "_$1_");
            }
        }
    }
}
