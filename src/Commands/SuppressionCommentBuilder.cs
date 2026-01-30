namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Provides methods for building and manipulating markdownlint suppression comments.
    /// This class contains pure functions that can be easily unit tested.
    /// </summary>
    public static class SuppressionCommentBuilder
    {
        private const string _disableLineDirective = "markdownlint-disable-line";

        /// <summary>
        /// Builds a new suppression comment with the specified rules.
        /// </summary>
        /// <param name="ruleCodes">One or more rule codes to suppress (e.g., "MD001", "MD009").</param>
        /// <returns>A complete suppression comment string.</returns>
        public static string BuildSuppressionComment(params string[] ruleCodes)
        {
            if (ruleCodes == null || ruleCodes.Length == 0)
                return $"<!-- {_disableLineDirective} -->";

            var rules = string.Join(" ", ruleCodes);
            return $"<!-- {_disableLineDirective} {rules} -->";
        }

        /// <summary>
        /// Builds a suppression comment with appropriate leading space based on line content.
        /// </summary>
        /// <param name="lineText">The current line text.</param>
        /// <param name="ruleCode">The rule code to suppress.</param>
        /// <returns>A suppression comment string, with leading space if line has content.</returns>
        public static string BuildSuppressionCommentForLine(string lineText, string ruleCode)
        {
            var spacing = string.IsNullOrWhiteSpace(lineText) ? "" : " ";
            return $"{spacing}<!-- {_disableLineDirective} {ruleCode} -->";
        }

        /// <summary>
        /// Appends a rule to an existing suppression comment.
        /// </summary>
        /// <param name="existingComment">The existing suppression comment.</param>
        /// <param name="ruleCode">The rule code to append.</param>
        /// <returns>The updated suppression comment, or the original if rule already exists.</returns>
        public static string AppendRuleToComment(string existingComment, string ruleCode)
        {
            if (string.IsNullOrEmpty(existingComment) || string.IsNullOrEmpty(ruleCode))
                return existingComment;

            // Check if the rule is already in the comment (case-insensitive)
            if (existingComment.IndexOf(ruleCode, StringComparison.OrdinalIgnoreCase) >= 0)
                return existingComment;

            // Extract existing rules from the comment
            var existingRules = ExtractRulesFromComment(existingComment);

            // Build new comment with the additional rule
            if (string.IsNullOrWhiteSpace(existingRules))
            {
                return $"<!-- {_disableLineDirective} {ruleCode} -->";
            }

            return $"<!-- {_disableLineDirective} {existingRules} {ruleCode} -->";
        }

        /// <summary>
        /// Checks if a line contains a disable-line suppression comment.
        /// </summary>
        /// <param name="lineText">The line text to check.</param>
        /// <returns>True if the line contains a disable-line comment.</returns>
        public static bool HasSuppressionComment(string lineText)
        {
            return !string.IsNullOrEmpty(lineText) &&
                   lineText.IndexOf(_disableLineDirective, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Finds the span of an existing suppression comment in a line.
        /// </summary>
        /// <param name="lineText">The line text to search.</param>
        /// <returns>A tuple of (startIndex, length), or (-1, 0) if not found.</returns>
        public static (int Start, int Length) FindSuppressionCommentSpan(string lineText)
        {
            if (string.IsNullOrEmpty(lineText))
                return (-1, 0);

            var commentStart = lineText.IndexOf("<!-- " + _disableLineDirective, StringComparison.OrdinalIgnoreCase);
            if (commentStart < 0)
            {
                // Try without space after <!--
                commentStart = lineText.IndexOf("<!--" + _disableLineDirective, StringComparison.OrdinalIgnoreCase);
            }

            if (commentStart < 0)
                return (-1, 0);

            var commentEnd = lineText.IndexOf("-->", commentStart);
            if (commentEnd < 0)
                return (-1, 0);

            var length = commentEnd - commentStart + 3; // +3 for "-->"
            return (commentStart, length);
        }

        /// <summary>
        /// Extracts the rule codes from an existing suppression comment.
        /// </summary>
        private static string ExtractRulesFromComment(string comment)
        {
            var directiveIndex = comment.IndexOf(_disableLineDirective, StringComparison.OrdinalIgnoreCase);
            if (directiveIndex < 0)
                return string.Empty;

            var afterDirective = comment.Substring(directiveIndex + _disableLineDirective.Length);

            // Remove trailing --> and whitespace
            var endIndex = afterDirective.IndexOf("-->");
            if (endIndex >= 0)
            {
                afterDirective = afterDirective.Substring(0, endIndex);
            }

            return afterDirective.Trim();
        }
    }
}
