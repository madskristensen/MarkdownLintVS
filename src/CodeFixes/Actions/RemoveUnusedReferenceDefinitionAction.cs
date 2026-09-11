using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    [FixForRule("MD053", SpanType = FixSpanType.Line, RequiresFactory = true)]
    public sealed class RemoveUnusedReferenceDefinitionAction(
        ITextSnapshot snapshot,
        Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove unused definition";

        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            return violation.ReplacementText == string.Empty
                ? new RemoveUnusedReferenceDefinitionAction(snapshot, span)
                : null;
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Delete(line.ExtentIncludingLineBreak.Span);
        }

        protected override string GetFixedText() => string.Empty;
    }
}
