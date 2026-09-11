using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Markdig.Syntax;

namespace MarkdownLintVS.Linting.Rules
{
    /// <summary>
    /// MD043: Required heading structure.
    /// </summary>
    public class MD043_RequiredHeadings : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD043");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity,
            CancellationToken cancellationToken = default)
        {
            string[] required = ParseRequiredHeadings(configuration.Value);
            if (required.Length == 0)
                yield break;

            List<(int Level, string Text, int Line)> actual = analysis.GetHeadings()
                .Select(heading => (
                    heading.Level,
                    GetHeadingText(analysis.GetLine(heading.Line), heading),
                    heading.Line))
                .ToList();
            bool matchCase = configuration.GetBoolParameter("match_case", defaultValue: false);
            if (MatchesRequiredHeadings(required, actual, matchCase))
                yield break;

            int problemIndex = Math.Min(actual.Count - 1, required.Length - 1);
            if (problemIndex >= 0)
            {
                int lineNumber = actual[problemIndex].Line;
                yield return CreateLineViolation(
                    lineNumber,
                    analysis.GetLine(lineNumber),
                    "Heading structure does not match the required headings",
                    severity);
            }
            else
            {
                int lineNumber = Math.Max(0, analysis.LineCount - 1);
                yield return CreateLineViolation(
                    lineNumber,
                    analysis.GetLine(lineNumber),
                    "Required heading is missing",
                    severity);
            }
        }

        internal static string[] ParseRequiredHeadings(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(';')
                    .Select(item => item.Trim())
                    .Where(item => item.Length > 0)
                    .ToArray();
        }

        internal static bool MatchesRequiredHeadings(
            IReadOnlyList<string> required,
            IReadOnlyList<(int Level, string Text, int Line)> actual,
            bool matchCase)
        {
            var memo = new Dictionary<(int Required, int Actual), bool>();
            return Match(0, 0);

            bool Match(int requiredIndex, int actualIndex)
            {
                if (requiredIndex == required.Count)
                    return actualIndex == actual.Count;

                if (memo.TryGetValue((requiredIndex, actualIndex), out bool cached))
                    return cached;

                string pattern = required[requiredIndex];
                bool result;
                if (pattern == "*" || pattern == "+")
                {
                    int minimum = pattern == "+" ? 1 : 0;
                    result = false;
                    for (int consumed = minimum; actualIndex + consumed <= actual.Count; consumed++)
                    {
                        if (Match(requiredIndex + 1, actualIndex + consumed))
                        {
                            result = true;
                            break;
                        }
                    }
                }
                else if (pattern == "?")
                {
                    result = actualIndex < actual.Count && Match(requiredIndex + 1, actualIndex + 1);
                }
                else
                {
                    result = actualIndex < actual.Count &&
                        HeadingMatches(pattern, actual[actualIndex], matchCase) &&
                        Match(requiredIndex + 1, actualIndex + 1);
                }

                memo[(requiredIndex, actualIndex)] = result;
                return result;
            }
        }

        private static bool HeadingMatches(
            string pattern,
            (int Level, string Text, int Line) actual,
            bool matchCase)
        {
            ParseHeadingPattern(pattern, out int requiredLevel, out string requiredText);
            StringComparison comparison = matchCase
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;
            return actual.Level == requiredLevel &&
                string.Equals(requiredText, actual.Text, comparison);
        }

        private static void ParseHeadingPattern(string pattern, out int level, out string text)
        {
            level = 0;
            while (level < pattern.Length && pattern[level] == '#')
            {
                level++;
            }

            level = Math.Max(1, Math.Min(6, level));
            text = pattern.Substring(Math.Min(pattern.Length, level)).Trim();
        }

        private static string GetHeadingText(string line, HeadingBlock heading)
        {
            if (heading.IsSetext)
                return line.Trim();

            return line.Trim().Trim('#').Trim();
        }
    }

    /// <summary>
    /// MD044: Proper names should have the correct capitalization.
    /// </summary>
    public class MD044_ProperNames : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD044");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity,
            CancellationToken cancellationToken = default)
        {
            string[] names = ParseProperNames(configuration.Value);
            if (names.Length == 0)
                yield break;

            bool includeCode = configuration.GetBoolParameter("code_blocks", defaultValue: true);
            bool includeHtml = configuration.GetBoolParameter("html_elements", defaultValue: true);
            foreach ((int lineNumber, string line) in analysis.GetAnalyzableLines(skipCodeBlocks: !includeCode))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!includeHtml && analysis.IsLineInHtmlBlock(lineNumber))
                    continue;

                foreach (string name in names)
                {
                    int searchStart = 0;
                    while (searchStart < line.Length)
                    {
                        int index = line.IndexOf(name, searchStart, StringComparison.OrdinalIgnoreCase);
                        if (index < 0)
                            break;

                        searchStart = index + name.Length;
                        if (!IsWordBoundary(line, index, name.Length) ||
                            (!includeCode && analysis.IsPositionInInlineCode(lineNumber, index)) ||
                            string.CompareOrdinal(line, index, name, 0, name.Length) == 0)
                        {
                            continue;
                        }

                        yield return CreateViolation(
                            lineNumber,
                            index,
                            index + name.Length,
                            $"Proper name '{name}' has incorrect capitalization",
                            severity,
                            $"Change to '{name}'",
                            name);
                    }
                }
            }
        }

        internal static string[] ParseProperNames(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(',')
                    .Select(item => item.Trim())
                    .Where(item => item.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
        }

        private static bool IsWordBoundary(string line, int start, int length)
        {
            bool startsAtBoundary = start == 0 || !IsWordCharacter(line[start - 1]);
            int end = start + length;
            bool endsAtBoundary = end == line.Length || !IsWordCharacter(line[end]);
            return startsAtBoundary && endsAtBoundary;
        }

        private static bool IsWordCharacter(char character) =>
            char.IsLetterOrDigit(character) || character == '_';
    }
}
