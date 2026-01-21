using System;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add a newline at end of file (MD047).
    /// </summary>
    [FixForRule("MD047", RequiresFactory = true)]
    public class AddFinalNewlineAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add newline at end of file";

        /// <summary>
        /// Creates the appropriate action based on whether there are multiple newlines or none.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            if (violation.Message.Contains("multiple"))
                return new RemoveExtraBlankLinesAction(snapshot, new Span(snapshot.Length - 1, 1));
            else
                return new AddFinalNewlineAction(snapshot, new Span(snapshot.Length, 0));
        }

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Insert(Snapshot.Length, Environment.NewLine);
        }

        protected override string GetFixedText()
        {
            return Environment.NewLine;
        }
    }
}
