using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to swap reversed link syntax (MD011).
    /// </summary>
    public class FixReversedLinkAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Fix reversed link syntax";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);
            var fixedText = FixReversedLink(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return FixReversedLink(Snapshot.GetText(Span));
        }

        private static string FixReversedLink(string text)
        {
            // Convert (url)[text] to [text](url)
            Match match = Regex.Match(text, @"\(([^)]+)\)\[([^\]]+)\]");
            if (match.Success)
            {
                return $"[{match.Groups[2].Value}]({match.Groups[1].Value})";
            }
            return text;
        }
    }
}
