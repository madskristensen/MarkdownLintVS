using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Helper class for inserting markdownlint suppression comments into markdown files.
    /// </summary>
    internal static class SuppressionHelper
    {
        /// <summary>
        /// Suppresses a markdownlint rule on the specified line by inserting a disable-line comment.
        /// If a disable-line comment already exists on the line, the rule is appended to it.
        /// </summary>
        /// <param name="textBuffer">The text buffer to modify.</param>
        /// <param name="lineNumber">The 0-based line number to suppress.</param>
        /// <param name="errorCode">The error code to suppress (e.g., "MD001").</param>
        public static void SuppressOnLine(ITextBuffer textBuffer, int lineNumber, string errorCode)
        {
            if (textBuffer == null || string.IsNullOrEmpty(errorCode))
                return;

            ITextSnapshot snapshot = textBuffer.CurrentSnapshot;

            // Validate line number
            if (lineNumber < 0 || lineNumber >= snapshot.LineCount)
                return;

            ITextSnapshotLine snapshotLine = snapshot.GetLineFromLineNumber(lineNumber);
            var lineText = snapshotLine.GetText();

            // Check if the line already has an inline suppression comment
            if (SuppressionCommentBuilder.HasSuppressionComment(lineText))
            {
                AddRuleToExistingComment(textBuffer, snapshotLine, lineText, errorCode);
            }
            else
            {
                InsertNewSuppressionComment(textBuffer, snapshotLine, lineText, errorCode);
            }
        }

        /// <summary>
        /// Suppresses a markdownlint rule at a specific snapshot point.
        /// </summary>
        /// <param name="textBuffer">The text buffer to modify.</param>
        /// <param name="point">A point on the line to suppress.</param>
        /// <param name="errorCode">The error code to suppress (e.g., "MD001").</param>
        public static void SuppressAtPoint(ITextBuffer textBuffer, SnapshotPoint point, string errorCode)
        {
            var lineNumber = point.GetContainingLine().LineNumber;
            SuppressOnLine(textBuffer, lineNumber, errorCode);
        }

        private static void InsertNewSuppressionComment(
            ITextBuffer textBuffer,
            ITextSnapshotLine line,
            string lineText,
            string errorCode)
        {
            var comment = SuppressionCommentBuilder.BuildSuppressionCommentForLine(lineText, errorCode);

            using (ITextEdit edit = textBuffer.CreateEdit())
            {
                edit.Insert(line.End.Position, comment);
                edit.Apply();
            }
        }

        private static void AddRuleToExistingComment(
            ITextBuffer textBuffer,
            ITextSnapshotLine line,
            string lineText,
            string errorCode)
        {
            (var commentStart, var commentLength) = SuppressionCommentBuilder.FindSuppressionCommentSpan(lineText);
            if (commentStart < 0)
                return;

            var existingComment = lineText.Substring(commentStart, commentLength);
            var newComment = SuppressionCommentBuilder.AppendRuleToComment(existingComment, errorCode);

            // If the comment didn't change (rule already exists), don't make an edit
            if (existingComment == newComment)
                return;

            using (ITextEdit edit = textBuffer.CreateEdit())
            {
                var absoluteStart = line.Start.Position + commentStart;
                edit.Replace(absoluteStart, commentLength, newComment);
                edit.Apply();
            }
        }
    }
}
