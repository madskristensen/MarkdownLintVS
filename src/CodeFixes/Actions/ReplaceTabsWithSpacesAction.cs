using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to replace all tabs with spaces on a line (MD010).
    /// </summary>
    [FixForRule("MD010", RequiresFactory = true)]
    public class ReplaceTabsWithSpacesAction(ITextSnapshot snapshot, Span span, int spacesPerTab = 4) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Replace tabs with spaces";

        /// <summary>
        /// Creates a fix action with default spaces per tab.
        /// </summary>
        public static MarkdownFixAction Create(ITextSnapshot snapshot, Span span, LintViolation violation)
        {
            return new ReplaceTabsWithSpacesAction(snapshot, span);
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return line.GetText().Replace("\t", new string(' ', spacesPerTab));
        }
    }
}
