using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to swap reversed link syntax (MD011).
    /// </summary>
    [FixForRule("MD011", SpanType = FixSpanType.Violation)]
    public class FixReversedLinkAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Fix reversed link syntax";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Replace(Span, GetFixedText());
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);
            Match match = Regex.Match(text, @"\(([^)]+)\)\[([^\]]+)\]");
            return match.Success ? $"[{match.Groups[2].Value}]({match.Groups[1].Value})" : text;
        }
    }
}
