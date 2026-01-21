using System.Text.RegularExpressions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change emphasis style between asterisk and underscore (MD049).
    /// </summary>
    [FixForRule("MD049", SpanType = FixSpanType.Violation, RequiresFactory = true)]
    public class ChangeEmphasisStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) 
        : ChangeInlineStyleAction(snapshot, span, targetStyle)
    {
        protected override string StyleTypeName => "emphasis";
        protected override string ToAsteriskPattern => @"(?<!\*)_([^_*]+)_(?!\*)";
        protected override string ToAsteriskReplacement => "*$1*";
        protected override string ToUnderscorePattern => @"(?<!\*)\*([^_*]+)\*(?!\*)";
        protected override string ToUnderscoreReplacement => "_$1_";

        /// <summary>
        /// Creates a fix action by extracting the expected style from the violation message.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            var style = ExtractExpectedStyle(violation.Message);
            return style != null ? new ChangeEmphasisStyleAction(snapshot, span, style) : null;
        }

        private static string ExtractExpectedStyle(string message)
        {
            Match match = Regex.Match(message, @"expected '([^']+)'|Expected:\s*(\w+)");
            if (match.Success)
                return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return null;
        }
    }
}
