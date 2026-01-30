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
            if (lineText.Contains("markdownlint-disable-line", StringComparison.OrdinalIgnoreCase))
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
            // Determine the appropriate spacing
            var spacing = string.IsNullOrWhiteSpace(lineText) ? "" : " ";
            var comment = $"{spacing}<!-- markdownlint-disable-line {errorCode} -->";

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
            // Find the existing markdownlint-disable-line comment
            var commentStart = lineText.IndexOf("<!-- markdownlint-disable-line", StringComparison.OrdinalIgnoreCase);
            if (commentStart < 0)
                return;

            var commentEnd = lineText.IndexOf("-->", commentStart);
            if (commentEnd < 0)
                return;

            var existingComment = lineText.Substring(commentStart, commentEnd - commentStart + 3);

            // Check if the rule is already in the comment
            if (existingComment.Contains(errorCode, StringComparison.OrdinalIgnoreCase))
                return;

            // Find where to insert the new rule (before the closing -->)
            // Check if there are already rules specified
            var directivePart = "markdownlint-disable-line";
            var directiveEnd = existingComment.IndexOf(directivePart, StringComparison.OrdinalIgnoreCase) + directivePart.Length;
            var afterDirective = existingComment.Substring(directiveEnd);

            // Check if there are existing rules (anything before the closing -->)
            var trimmed = afterDirective.TrimEnd().TrimEnd('-', '>').Trim();
            string newComment;

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                // No rules specified yet, add the rule
                newComment = $"<!-- markdownlint-disable-line {errorCode} -->";
            }
            else
            {
                // Rules exist, append our rule
                newComment = $"<!-- markdownlint-disable-line {trimmed} {errorCode} -->";
            }

            using (ITextEdit edit = textBuffer.CreateEdit())
            {
                var absoluteStart = line.Start.Position + commentStart;
                edit.Replace(absoluteStart, existingComment.Length, newComment);
                edit.Apply();
            }
        }
    }
}
