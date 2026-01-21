using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove spaces inside code span elements (MD038).
    /// </summary>
    [FixForRule("MD038", SpanType = FixSpanType.Violation)]
    public class RemoveSpaceInCodeSpanAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove spaces inside code span";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Replace(Span, GetFixedText());
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);
            var backtickCount = 0;
            for (var i = 0; i < text.Length && text[i] == '`'; i++)
                backtickCount++;

            if (backtickCount > 0 && text.Length > backtickCount * 2)
            {
                var delimiter = new string('`', backtickCount);
                var content = text.Substring(backtickCount, text.Length - backtickCount * 2);
                return delimiter + content.Trim() + delimiter;
            }
            return text;
        }
    }
}
