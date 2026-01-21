using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change strong/bold style between asterisk and underscore (MD050).
    /// </summary>
    [FixForRule("MD050", SpanType = FixSpanType.Violation, RequiresFactory = true)]
    public class ChangeStrongStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) 
        : ChangeInlineStyleAction(snapshot, span, targetStyle)
    {
        protected override string StyleTypeName => "strong";
        protected override string ToAsteriskPattern => @"__([^_]+)__";
        protected override string ToAsteriskReplacement => "**$1**";
        protected override string ToUnderscorePattern => @"\*\*([^*]+)\*\*";
        protected override string ToUnderscoreReplacement => "__$1__";

        /// <summary>
        /// Creates a fix action by extracting the expected style from the violation message.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            var style = ViolationMessageParser.ExtractExpectedStyle(violation.Message);
            return style != null ? new ChangeStrongStyleAction(snapshot, span, style) : null;
        }
    }
}
