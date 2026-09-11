using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    [FixForRule("MD005", SpanType = FixSpanType.Line, RequiresFactory = true)]
    [FixForRule("MD007", SpanType = FixSpanType.Line, RequiresFactory = true)]
    [FixForRule("MD051", SpanType = FixSpanType.Violation, RequiresFactory = true)]
    [FixForRule("MD054", SpanType = FixSpanType.Violation, RequiresFactory = true)]
    public sealed class ReplaceWithSuggestedTextAction(
        ITextSnapshot snapshot,
        Span span,
        string replacement,
        string displayText) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => displayText;

        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            return violation.ReplacementText == null
                ? null
                : new ReplaceWithSuggestedTextAction(
                    snapshot,
                    span,
                    violation.ReplacementText,
                    violation.FixDescription ?? "Apply suggested fix");
        }

        public override void ApplyFix(ITextEdit edit) => edit.Replace(Span, replacement);

        protected override string GetFixedText() => replacement;
    }
}
