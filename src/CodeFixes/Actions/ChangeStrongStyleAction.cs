using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change strong/bold style between asterisk and underscore (MD050).
    /// </summary>
    public class ChangeStrongStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => $"Change strong to {targetStyle}";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Replace(Span, GetFixedText());
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);

            if (targetStyle == "asterisk")
                return Regex.Replace(text, @"__([^_]+)__", "**$1**");
            else
                return Regex.Replace(text, @"\*\*([^*]+)\*\*", "__$1__");
        }
    }
}
