using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove spaces inside link text (MD039).
    /// </summary>
    [FixForRule("MD039", SpanType = FixSpanType.Violation)]
    public class RemoveSpaceInLinkTextAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove spaces inside link text";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Replace(Span, GetFixedText());
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);
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
