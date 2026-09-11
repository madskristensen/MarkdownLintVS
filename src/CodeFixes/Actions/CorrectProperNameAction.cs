using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    [FixForRule("MD044", SpanType = FixSpanType.Violation, RequiresFactory = true)]
    public sealed class CorrectProperNameAction(
        ITextSnapshot snapshot,
        Span span,
        string replacement) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => $"Change to '{replacement}'";

        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            return string.IsNullOrEmpty(violation.ReplacementText)
                ? null
                : new CorrectProperNameAction(snapshot, span, violation.ReplacementText);
        }

        public override void ApplyFix(ITextEdit edit) => edit.Replace(Span, replacement);

        protected override string GetFixedText() => replacement;
    }
}
