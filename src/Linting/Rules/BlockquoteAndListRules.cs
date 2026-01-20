using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarkdownLintVS.Linting.Rules
{
    /// <summary>
    /// MD027: Multiple spaces after blockquote symbol.
    /// </summary>
    public class MD027_NoMultipleSpaceBlockquote : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD027");
        public override RuleInfo Info => _info;

        private static readonly Regex MultipleSpaceBlockquotePattern = new(
            @"^(\s*>)+\s{2,}",
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
                if (MultipleSpaceBlockquotePattern.IsMatch(line))
                {
                    yield return CreateLineViolation(
                        i,
                        line,
                        "Multiple spaces after blockquote symbol",
                        severity,
                        "Use single space after '>'");
                }
            }
        }
    }

    /// <summary>
    /// MD028: Blank line inside blockquote.
    /// </summary>
    public class MD028_NoBlanksBlockquote : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD028");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var inBlockquote = false;
            var lastBlockquoteLine = -1;

            for (var i = 0; i < analysis.LineCount; i++)
            {
                if (analysis.IsLineInCodeBlock(i) || analysis.IsLineInFrontMatter(i))
                    continue;

                var line = analysis.GetLine(i);
                var isBlockquoteLine = line.TrimStart().StartsWith(">");

                if (isBlockquoteLine)
                {
                    if (inBlockquote && i > lastBlockquoteLine + 1)
                    {
                        // Found a blank line between blockquote lines
                        yield return CreateLineViolation(
                            lastBlockquoteLine + 1,
                            analysis.GetLine(lastBlockquoteLine + 1),
                            "Blank line inside blockquote",
                            severity,
                            "Remove blank line or use '>' prefix");
                    }
                    inBlockquote = true;
                    lastBlockquoteLine = i;
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    inBlockquote = false;
                }
            }
        }
    }

    /// <summary>
    /// MD029: Ordered list item prefix.
    /// </summary>
    public class MD029_OlPrefix : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD029");
        public override RuleInfo Info => _info;

        private static readonly Regex OrderedListPattern = new(
            @"^\s*(\d+)\.\s",
            RegexOptions.Compiled);

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var style = configuration.GetStringParameter("style", "one_or_ordered");
            if (style == "false")
                yield break;

            foreach (ListBlock list in analysis.GetLists().Where(l => l.IsOrdered))
            {
                var expectedNumber = style == "zero" ? 0 : 1;
                var firstItem = true;
                string detectedStyle = null;

                foreach (Block item in list)
                {
                    if (item is ListItemBlock listItem)
                    {
                        var line = analysis.GetLine(listItem.Line);
                        Match match = OrderedListPattern.Match(line);
                        if (match.Success)
                        {
                            var number = int.Parse(match.Groups[1].Value);

                            if (style == "one_or_ordered" && firstItem)
                            {
                                detectedStyle = number == 1 ? "one" : "ordered";
                                expectedNumber = number;
                            }
                            else if (style == "one")
                            {
                                if (number != 1)
                                {
                                    yield return CreateLineViolation(
                                        listItem.Line,
                                        line,
                                        $"Ordered list item prefix should be '1' (found '{number}')",
                                        severity);
                                }
                            }
                            else if (style == "ordered" || (style == "one_or_ordered" && detectedStyle == "ordered"))
                            {
                                if (number != expectedNumber)
                                {
                                    yield return CreateLineViolation(
                                        listItem.Line,
                                        line,
                                        $"Ordered list item prefix should be '{expectedNumber}' (found '{number}')",
                                        severity);
                                }
                                expectedNumber++;
                            }
                            else if (style == "zero")
                            {
                                if (number != 0)
                                {
                                    yield return CreateLineViolation(
                                        listItem.Line,
                                        line,
                                        $"Ordered list item prefix should be '0' (found '{number}')",
                                        severity);
                                }
                            }

                            firstItem = false;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// MD030: Spaces after list markers.
    /// </summary>
    public class MD030_ListMarkerSpace : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD030");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var ulSingle = configuration.GetIntParameter("ul_single", 1);
            var olSingle = configuration.GetIntParameter("ol_single", 1);
            var ulMulti = configuration.GetIntParameter("ul_multi", 1);
            var olMulti = configuration.GetIntParameter("ol_multi", 1);

            foreach (ListBlock list in analysis.GetLists())
            {
                var isOrdered = list.IsOrdered;
                var isMulti = HasMultiParagraphItems(list, analysis);
                var expectedSpaces = isOrdered 
                    ? (isMulti ? olMulti : olSingle)
                    : (isMulti ? ulMulti : ulSingle);

                foreach (Block item in list)
                {
                    if (item is ListItemBlock listItem)
                    {
                        var line = analysis.GetLine(listItem.Line);
                        var spaces = CountSpacesAfterMarker(line, isOrdered);

                        if (spaces != expectedSpaces)
                        {
                            yield return CreateLineViolation(
                                listItem.Line,
                                line,
                                $"Expected {expectedSpaces} space(s) after list marker (found {spaces})",
                                severity);
                        }
                    }
                }
            }
        }

        private bool HasMultiParagraphItems(ListBlock list, MarkdownDocumentAnalysis analysis)
        {
            foreach (Block item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var paragraphCount = 0;
                    foreach (Block child in listItem)
                    {
                        if (child is ParagraphBlock)
                            paragraphCount++;
                    }
                    if (paragraphCount > 1)
                        return true;
                }
            }
            return false;
        }

        private int CountSpacesAfterMarker(string line, bool isOrdered)
        {
            var trimmed = line.TrimStart();
            int markerEnd;

            if (isOrdered)
            {
                Match match = Regex.Match(trimmed, @"^\d+\.");
                if (!match.Success) return 1;
                markerEnd = match.Length;
            }
            else
            {
                if (trimmed.Length == 0 || !"*+-".Contains(trimmed[0]))
                    return 1;
                markerEnd = 1;
            }

            var spaces = 0;
            for (var i = markerEnd; i < trimmed.Length && trimmed[i] == ' '; i++)
            {
                spaces++;
            }
            return spaces;
        }
    }

    /// <summary>
    /// MD031: Fenced code blocks should be surrounded by blank lines.
    /// </summary>
    public class MD031_BlanksAroundFences : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD031");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var listItems = configuration.GetBoolParameter("list_items", true);

            foreach (FencedCodeBlock codeBlock in analysis.GetFencedCodeBlocks())
            {
                var startLine = codeBlock.Line;
                var endLine = analysis.GetBlockEndLine(codeBlock);

                // Check line before
                if (startLine > 0 && !analysis.IsBlankLine(startLine - 1))
                {
                    if (!listItems || !IsInListItem(startLine - 1, analysis))
                    {
                        yield return CreateLineViolation(
                            startLine,
                            analysis.GetLine(startLine),
                            "Fenced code blocks should be surrounded by blank lines",
                            severity,
                            "Add blank line before code block");
                    }
                }

                // Check line after
                if (endLine < analysis.LineCount - 1 && !analysis.IsBlankLine(endLine + 1))
                {
                    if (!listItems || !IsInListItem(endLine + 1, analysis))
                    {
                        yield return CreateLineViolation(
                            endLine,
                            analysis.GetLine(endLine),
                            "Fenced code blocks should be surrounded by blank lines",
                            severity,
                            "Add blank line after code block");
                    }
                }
            }
        }

        private bool IsInListItem(int lineNumber, MarkdownDocumentAnalysis analysis)
        {
            foreach (ListItemBlock listItem in analysis.GetListItems())
            {
                if (lineNumber >= listItem.Line && lineNumber <= analysis.GetBlockEndLine(listItem))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// MD032: Lists should be surrounded by blank lines.
    /// </summary>
    public class MD032_BlanksAroundLists : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD032");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            foreach (ListBlock list in analysis.GetLists())
            {
                // Only check top-level lists
                if (list.Parent is ListItemBlock)
                    continue;

                var startLine = list.Line;
                var endLine = analysis.GetBlockEndLine(list);

                // Check line before
                if (startLine > 0 && !analysis.IsBlankLine(startLine - 1))
                {
                    yield return CreateLineViolation(
                        startLine,
                        analysis.GetLine(startLine),
                        "Lists should be surrounded by blank lines",
                        severity,
                        "Add blank line before list");
                }

                // Check line after
                if (endLine < analysis.LineCount - 1 && !analysis.IsBlankLine(endLine + 1))
                {
                    yield return CreateLineViolation(
                        endLine,
                        analysis.GetLine(endLine),
                        "Lists should be surrounded by blank lines",
                        severity,
                        "Add blank line after list");
                }
            }
        }
    }
}
