using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove extra trailing newlines at end of file (MD047).
    /// Removes all trailing blank lines except one, leaving a single newline at EOF.
    /// </summary>
    [FixForRule("MD047")]
    public class RemoveExtraTrailingNewlinesAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove extra newlines at end of file";

        public override void ApplyFix(ITextEdit edit)
        {
            // Find where trailing blank lines start (after the last non-blank line)
            var lastContentEnd = FindLastContentEnd();

            // Delete from after the first newline following content to the end
            // This preserves exactly one newline at the end of the last content line
            if (lastContentEnd < Snapshot.Length)
            {
                // Find the end of the first newline after content
                var deleteStart = lastContentEnd;
                if (deleteStart < Snapshot.Length && Snapshot[deleteStart] == '\r')
                    deleteStart++;
                if (deleteStart < Snapshot.Length && Snapshot[deleteStart] == '\n')
                    deleteStart++;

                // Delete everything after that first newline
                if (deleteStart < Snapshot.Length)
                {
                    edit.Delete(deleteStart, Snapshot.Length - deleteStart);
                }
            }
        }

        /// <summary>
        /// Finds the position immediately after the last non-whitespace content.
        /// </summary>
        private int FindLastContentEnd()
        {
            var text = Snapshot.GetText();
            var lastContentPos = text.Length - 1;

            // Walk backwards to find the last non-newline character
            while (lastContentPos >= 0)
            {
                var c = text[lastContentPos];
                if (c != '\r' && c != '\n')
                    break;
                lastContentPos--;
            }

            // Return position after the last content character
            return lastContentPos + 1;
        }

        protected override string GetFixedText()
        {
            // Preview: show that we're keeping the content with a single trailing newline
            var lastContentEnd = FindLastContentEnd();
            if (lastContentEnd > 0)
            {
                // Get the last line of content
                ITextSnapshotLine lastContentLine = Snapshot.GetLineFromPosition(lastContentEnd - 1);
                return lastContentLine.GetText() + "\r\n";
            }
            return "\r\n";
        }
    }
}
