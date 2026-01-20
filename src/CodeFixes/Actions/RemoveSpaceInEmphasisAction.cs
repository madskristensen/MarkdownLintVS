using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove spaces inside emphasis markers (MD037).
    /// </summary>
    public class RemoveSpaceInEmphasisAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove spaces inside emphasis";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Replace(Span, GetFixedText());
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);
            text = Regex.Replace(text, @"(\*+|_+)\s+", "$1");
            text = Regex.Replace(text, @"\s+(\*+|_+)$", "$1");
            return text;
        }
    }
}
