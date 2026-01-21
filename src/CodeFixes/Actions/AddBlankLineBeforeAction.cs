using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add a blank line before content (MD022, MD031, MD032, MD058).
    /// </summary>
    [FixForRule("MD022", RequiresFactory = true)]
    [FixForRule("MD031", RequiresFactory = true)]
    [FixForRule("MD058", RequiresFactory = true)]
    public class AddBlankLineBeforeAction(ITextSnapshot snapshot, Span span)
        : BlankLineActionBase(snapshot, span, BlankLinePosition.Before)
    {
        /// <summary>
        /// Creates the appropriate blank line action based on the violation message.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            var fixDesc = violation.FixDescription ?? violation.Message;
            var position = ViolationMessageParser.ExtractBlankLinePosition(fixDesc);

            return position switch
            {
                "before" => new AddBlankLineBeforeAction(snapshot, span),
                "after" => new AddBlankLineAfterAction(snapshot, span),
                _ => null
            };
        }
    }
}
