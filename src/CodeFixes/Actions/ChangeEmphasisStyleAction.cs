using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change emphasis style between asterisk and underscore (MD049).
    /// </summary>
    public class ChangeEmphasisStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => $"Change emphasis to {targetStyle}";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Replace(Span, GetFixedText());
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);

            if (targetStyle == "asterisk")
                return Regex.Replace(text, @"(?<!\*)_([^_*]+)_(?!\*)", "*$1*");
            else
                return Regex.Replace(text, @"(?<!\*)\*([^_*]+)\*(?!\*)", "_$1_");
        }
    }
}
