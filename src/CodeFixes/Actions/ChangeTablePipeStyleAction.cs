using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to normalize table pipe style on a row (MD055).
    /// Adds or removes leading/trailing pipes to match the expected style.
    /// </summary>
    [FixForRule("MD055", RequiresFactory = true)]
    public class ChangeTablePipeStyleAction(ITextSnapshot snapshot, Span span, string targetStyle)
        : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => targetStyle switch
        {
            "leading_and_trailing" => "Add leading and trailing pipes",
            "leading_only" => "Add leading pipe",
            "trailing_only" => "Add trailing pipe",
            "no_leading_or_trailing" => "Remove leading and trailing pipes",
            _ => "Fix table pipe style"
        };

        /// <summary>
        /// Creates a fix action by extracting the expected pipe style from the violation message.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            var style = ViolationMessageParser.ExtractExpectedPipeStyle(violation.Message);
            return style != null ? new ChangeTablePipeStyleAction(snapshot, span, style) : null;
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var trimmed = text.Trim();

            // Strip existing leading/trailing pipes to get the inner content
            var inner = trimmed;
            if (inner.StartsWith("|"))
                inner = inner.Substring(1);
            if (inner.EndsWith("|"))
                inner = inner.Substring(0, inner.Length - 1);

            // Preserve leading whitespace from original line
            var leadingWhitespace = text.Substring(0, text.Length - text.TrimStart().Length);

            return targetStyle switch
            {
                "leading_and_trailing" => $"{leadingWhitespace}|{inner}|",
                "leading_only" => $"{leadingWhitespace}|{inner}",
                "trailing_only" => $"{leadingWhitespace}{inner}|",
                "no_leading_or_trailing" => $"{leadingWhitespace}{inner}",
                _ => text
            };
        }
    }
}
