using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove spaces inside link text (MD039).
    /// </summary>
    public class RemoveSpaceInLinkTextAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove spaces inside link text";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);
            var fixedText = RemoveSpacesInLinkText(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return RemoveSpacesInLinkText(Snapshot.GetText(Span));
        }

        private static string RemoveSpacesInLinkText(string text)
        {
            // Match [text](url) or [text][ref] pattern and trim text inside brackets
            Match match = Regex.Match(text, @"^\[(\s*)([^\]]*?)(\s*)\](.*)$");
            if (match.Success)
            {
                var content = match.Groups[2].Value;
                var rest = match.Groups[4].Value;
                return "[" + content + "]" + rest;
            }
            return text;
        }
    }
}
