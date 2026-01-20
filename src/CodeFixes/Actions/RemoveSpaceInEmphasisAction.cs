using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove spaces inside emphasis markers (MD037).
    /// </summary>
    public class RemoveSpaceInEmphasisAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove spaces inside emphasis";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);
            var fixedText = RemoveSpacesInEmphasis(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return RemoveSpacesInEmphasis(Snapshot.GetText(Span));
        }

        private static string RemoveSpacesInEmphasis(string text)
        {
            // Handle *text* or _text_ with spaces
            // Remove space after opening marker
            text = Regex.Replace(text, @"(\*+|_+)\s+", "$1");
            // Remove space before closing marker
            text = Regex.Replace(text, @"\s+(\*+|_+)$", "$1");
            return text;
        }
    }
}
