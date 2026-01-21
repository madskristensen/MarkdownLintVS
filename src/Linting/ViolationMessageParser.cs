using System.Text.RegularExpressions;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Utility class for parsing information from lint violation messages.
    /// Extracts expected values from standardized message formats.
    /// </summary>
    public static class ViolationMessageParser
    {
        // Pre-compiled regex patterns for performance
        private static readonly Regex _expectedNumberPattern = new Regex(
            @"should be '(\d+)'",
            RegexOptions.Compiled);

        private static readonly Regex _expectedValuePattern = new Regex(
            @"expected '([^']+)'",
            RegexOptions.Compiled);

        private static readonly Regex _expectedColonPattern = new Regex(
            @"Expected:\s*(\w+)",
            RegexOptions.Compiled);
        /// <summary>
        /// Extracts the expected list marker character from a violation message.
        /// </summary>
        /// <param name="message">The violation message (e.g., "expected 'dash'", "should use asterisk").</param>
        /// <returns>The expected marker character ('-', '*', or '+'), or null if not found.</returns>
        public static char? ExtractExpectedMarker(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;

            if (message.Contains("expected 'dash'") || message.Contains("should use dash"))
                return '-';
            if (message.Contains("expected 'asterisk'") || message.Contains("should use asterisk"))
                return '*';
            if (message.Contains("expected 'plus'") || message.Contains("should use plus"))
                return '+';
            
            return null;
        }

        /// <summary>
        /// Extracts the expected number from a violation message.
        /// </summary>
        /// <param name="message">The violation message (e.g., "should be '1'").</param>
        /// <returns>The expected number, or null if not found.</returns>
        public static int? ExtractExpectedNumber(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;

            Match match = _expectedNumberPattern.Match(message);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var number))
                return number;

            return null;
        }

        /// <summary>
        /// Extracts the expected style from a violation message.
        /// </summary>
        /// <param name="message">The violation message (e.g., "expected 'backtick'", "Expected: asterisk").</param>
        /// <returns>The expected style string, or null if not found.</returns>
        public static string ExtractExpectedStyle(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;

            // Try "expected 'value'" pattern first
            Match match = _expectedValuePattern.Match(message);
            if (match.Success)
                return match.Groups[1].Value;

            // Try "Expected: value" pattern
            match = _expectedColonPattern.Match(message);
            if (match.Success)
                return match.Groups[1].Value;

            // Keyword-based fallback for common styles
            if (message.Contains("expected backtick") || message.Contains("should be backtick"))
                return "backtick";
            if (message.Contains("expected tilde") || message.Contains("should be tilde"))
                return "tilde";
            if (message.Contains("expected asterisk") || message.Contains("should be asterisk"))
                return "asterisk";
            if (message.Contains("expected underscore") || message.Contains("should be underscore"))
                return "underscore";

            // Horizontal rule patterns
            if (message.Contains("---"))
                return "---";
            if (message.Contains("***"))
                return "***";
            if (message.Contains("___"))
                return "___";

            return null;
        }

        /// <summary>
        /// Determines if a blank line should be added before or after based on the violation message.
        /// </summary>
        /// <param name="message">The violation message or fix description.</param>
        /// <returns>"before", "after", or null if not determinable.</returns>
        public static string ExtractBlankLinePosition(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;

            if (message.Contains("before"))
                return "before";
            if (message.Contains("after"))
                return "after";

            return null;
        }

        /// <summary>
        /// Determines if the message indicates multiple newlines at end of file.
        /// </summary>
        /// <param name="message">The violation message.</param>
        /// <returns>True if the message indicates multiple newlines, false otherwise.</returns>
        public static bool IsMultipleNewlines(string message)
        {
            return !string.IsNullOrEmpty(message) && message.Contains("multiple");
        }
    }
}
