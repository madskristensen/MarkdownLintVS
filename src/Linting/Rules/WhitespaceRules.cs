using System.Collections.Generic;
using System.Text.RegularExpressions;
using Markdig.Syntax;

namespace MarkdownLintVS.Linting.Rules
{
    /// <summary>
    /// MD009: Trailing spaces.
    /// </summary>
    public class MD009_NoTrailingSpaces : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD009");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var brSpaces = configuration.GetIntParameter("br_spaces", 2);
            var listItemEmptyLines = configuration.GetBoolParameter("list_item_empty_lines", false);
            var strict = configuration.GetBoolParameter("strict", false);

            for (var i = 0; i < analysis.LineCount; i++)
            {
                if (analysis.IsLineInCodeBlock(i) || analysis.IsLineInFrontMatter(i))
                    continue;

                var line = analysis.GetLine(i);
                var trailingSpaces = CountTrailingSpaces(line);

                if (trailingSpaces > 0)
                {
                    // Allow exactly brSpaces for line breaks (unless strict mode)
                    if (!strict && trailingSpaces == brSpaces)
                        continue;

                    // Check for list item empty lines
                    if (listItemEmptyLines && string.IsNullOrWhiteSpace(line))
                        continue;

                    yield return CreateViolation(
                        i,
                        line.Length - trailingSpaces,
                        line.Length,
                        $"Trailing spaces ({trailingSpaces} found)",
                        severity,
                        "Remove trailing spaces");
                }
            }
        }

        private int CountTrailingSpaces(string line)
        {
            var count = 0;
            for (var i = line.Length - 1; i >= 0; i--)
            {
                if (line[i] == ' ')
                    count++;
                else
                    break;
            }
            return count;
        }
    }

    /// <summary>
    /// MD010: Hard tabs.
    /// </summary>
    public class MD010_NoHardTabs : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD010");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var codeBlocks = configuration.GetBoolParameter("code_blocks", true);
            var ignoreCodeLanguages = configuration.GetStringParameter("ignore_code_languages", "");
            var spacesPerTab = configuration.GetIntParameter("spaces_per_tab", 4);

            for (var i = 0; i < analysis.LineCount; i++)
            {
                if (analysis.IsLineInFrontMatter(i))
                    continue;

                if (!codeBlocks && analysis.IsLineInCodeBlock(i))
                    continue;

                var line = analysis.GetLine(i);
                var tabIndex = line.IndexOf('\t');

                while (tabIndex >= 0)
                {
                    yield return CreateViolation(
                        i,
                        tabIndex,
                        tabIndex + 1,
                        "Hard tabs",
                        severity,
                        $"Replace tab with {spacesPerTab} spaces");

                    tabIndex = line.IndexOf('\t', tabIndex + 1);
                }
            }
        }
    }

    /// <summary>
    /// MD011: Reversed link syntax.
    /// </summary>
    public class MD011_NoReversedLinks : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD011");
        public override RuleInfo Info => _info;

        private static readonly Regex _reversedLinkPattern = new(
            @"\([^)]+\)\[[^\]]+\]",
            RegexOptions.Compiled);

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            for (var i = 0; i < analysis.LineCount; i++)
            {
                if (analysis.IsLineInCodeBlock(i) || analysis.IsLineInFrontMatter(i))
                    continue;

                var line = analysis.GetLine(i);
                MatchCollection matches = _reversedLinkPattern.Matches(line);

                foreach (Match match in matches)
                {
                    yield return CreateViolation(
                        i,
                        match.Index,
                        match.Index + match.Length,
                        "Reversed link syntax",
                        severity,
                        "Swap link text and URL");
                }
            }
        }
    }

    /// <summary>
    /// MD012: Multiple consecutive blank lines.
    /// </summary>
    public class MD012_NoMultipleBlanks : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD012");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var maximum = configuration.GetIntParameter("maximum", 1);
            var consecutiveBlanks = 0;

            for (var i = 0; i < analysis.LineCount; i++)
            {
                if (analysis.IsLineInCodeBlock(i) || analysis.IsLineInFrontMatter(i))
                {
                    consecutiveBlanks = 0;
                    continue;
                }

                if (analysis.IsBlankLine(i))
                {
                    consecutiveBlanks++;
                    if (consecutiveBlanks > maximum)
                    {
                        yield return CreateLineViolation(
                            i,
                            analysis.GetLine(i),
                            $"Multiple consecutive blank lines ({consecutiveBlanks} found, maximum {maximum} allowed)",
                            severity,
                            "Remove extra blank lines");
                    }
                }
                else
                {
                    consecutiveBlanks = 0;
                }
            }
        }
    }

    /// <summary>
    /// MD013: Line length.
    /// </summary>
    public class MD013_LineLength : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD013");
        public override RuleInfo Info => _info;

        private static readonly Regex _urlPattern = new(
            @"https?://[^\s\)]+",
            RegexOptions.Compiled);

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var lineLength = configuration.GetIntParameter("line_length", 80);
            var headingLineLength = configuration.GetIntParameter("heading_line_length", lineLength);
            var codeBlockLineLength = configuration.GetIntParameter("code_block_line_length", lineLength);
            var codeBlocks = configuration.GetBoolParameter("code_blocks", true);
            var tables = configuration.GetBoolParameter("tables", true);
            var headings = configuration.GetBoolParameter("headings", true);
            var strict = configuration.GetBoolParameter("strict", false);
            var stern = configuration.GetBoolParameter("stern", false);

            for (var i = 0; i < analysis.LineCount; i++)
            {
                if (analysis.IsLineInFrontMatter(i))
                    continue;

                var line = analysis.GetLine(i);
                var maxLength = lineLength;
                var isCodeBlock = analysis.IsLineInCodeBlock(i);
                var isHeading = line.TrimStart().StartsWith("#");

                if (isCodeBlock)
                {
                    if (!codeBlocks)
                        continue;
                    maxLength = codeBlockLineLength;
                }
                else if (isHeading)
                {
                    if (!headings)
                        continue;
                    maxLength = headingLineLength;
                }

                if (!tables && line.TrimStart().StartsWith("|"))
                    continue;

                var effectiveLength = GetEffectiveLength(line, strict);

                if (effectiveLength > maxLength)
                {
                    yield return CreateViolation(
                        i,
                        maxLength,
                        line.Length,
                        $"Line length is {effectiveLength} (maximum {maxLength})",
                        severity);
                }
            }
        }

        private static int GetEffectiveLength(string line, bool strict)
        {
            if (strict)
                return line.Length;

            // In non-strict mode, don't count URLs
            var length = line.Length;
            foreach (Match match in _urlPattern.Matches(line))
            {
                length -= match.Length;
            }
            return length;
        }
    }

    /// <summary>
    /// MD014: Dollar signs used before commands without showing output.
    /// </summary>
    public class MD014_CommandsShowOutput : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD014");
        public override RuleInfo Info => _info;

        private static readonly Regex _dollarCommandPattern = new(
            @"^\s*\$\s+",
            RegexOptions.Compiled);

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            foreach (FencedCodeBlock codeBlock in analysis.GetFencedCodeBlocks())
            {
                var startLine = codeBlock.Line;
                var endLine = analysis.GetBlockEndLine(codeBlock);
                var hasOutput = false;
                var dollarLines = new List<int>();

                for (var i = startLine + 1; i < endLine; i++)
                {
                    var line = analysis.GetLine(i);
                    if (_dollarCommandPattern.IsMatch(line))
                    {
                        dollarLines.Add(i);
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        hasOutput = true;
                    }
                }

                if (!hasOutput && dollarLines.Count > 0)
                {
                    foreach (var lineNum in dollarLines)
                    {
                        yield return CreateLineViolation(
                            lineNum,
                            analysis.GetLine(lineNum),
                            "Dollar signs used before commands without showing output",
                            severity,
                            "Remove dollar sign or add command output");
                    }
                }
            }
        }
    }
}
