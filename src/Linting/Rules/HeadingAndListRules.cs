using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig.Syntax;

namespace MarkdownLintVS.Linting.Rules
{
    /// <summary>
    /// MD001: Heading levels should only increment by one level at a time.
    /// </summary>
    public class MD001_HeadingIncrement : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD001");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var headings = analysis.GetHeadings().OrderBy(h => h.Line).ToList();
            var previousLevel = 0;

            foreach (HeadingBlock heading in headings)
            {
                if (previousLevel > 0 && heading.Level > previousLevel + 1)
                {
                    var line = analysis.GetLine(heading.Line);
                    yield return CreateLineViolation(
                        heading.Line,
                        line,
                        $"Heading level should increment by one level at a time (expected h{previousLevel + 1}, found h{heading.Level})",
                        severity);
                }
                previousLevel = heading.Level;
            }
        }
    }

    /// <summary>
    /// MD003: Heading style should be consistent.
    /// </summary>
    public class MD003_HeadingStyle : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD003");
        public override RuleInfo Info => _info;

        private static readonly Regex _atxClosedPattern = new(@"^#{1,6}\s+.+\s+#{1,6}\s*$", RegexOptions.Compiled);

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var style = configuration.GetStringParameter("style", "consistent");
            if (style == "false")
                yield break;

            string detectedStyle = null;

            foreach (HeadingBlock heading in analysis.GetHeadings())
            {
                var line = analysis.GetLine(heading.Line);
                var currentStyle = GetHeadingStyle(line, heading);

                if (style == "consistent")
                {
                    if (detectedStyle == null)
                    {
                        detectedStyle = currentStyle;
                    }
                    else if (currentStyle != detectedStyle)
                    {
                        // Handle setext_with_atx style
                        if (detectedStyle == "setext" && currentStyle == "atx" && heading.Level > 2)
                            continue;

                        yield return CreateLineViolation(
                            heading.Line,
                            line,
                            $"Heading style should be consistent (expected {detectedStyle}, found {currentStyle})",
                            severity);
                    }
                }
                else if (currentStyle != style)
                {
                    yield return CreateLineViolation(
                        heading.Line,
                        line,
                        $"Heading style should be {style} (found {currentStyle})",
                        severity);
                }
            }
        }

        private string GetHeadingStyle(string line, HeadingBlock heading)
        {
            if (heading.IsSetext)
                return "setext";

            if (_atxClosedPattern.IsMatch(line))
                return "atx_closed";

            return "atx";
        }
    }

    /// <summary>
    /// MD004: Unordered list style should be consistent.
    /// </summary>
    public class MD004_UlStyle : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD004");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var style = configuration.GetStringParameter("style", "consistent");
            if (style == "false")
                yield break;

            char? detectedMarker = null;
            var lastLevelMarker = new Dictionary<int, char>();

            foreach (ListBlock list in analysis.GetLists().Where(l => l.BulletType != '1'))
            {
                foreach (Block item in list)
                {
                    if (item is ListItemBlock listItem)
                    {
                        var line = analysis.GetLine(listItem.Line);
                        var marker = GetListMarker(line);
                        if (marker == null) continue;

                        var indent = GetIndentLevel(line);

                        if (style == "consistent")
                        {
                            if (detectedMarker == null)
                            {
                                detectedMarker = marker;
                            }
                            else if (marker != detectedMarker)
                            {
                                yield return CreateLineViolation(
                                    listItem.Line,
                                    line,
                                    $"Unordered list style should be consistent (expected '{GetMarkerName(detectedMarker.Value)}', found '{GetMarkerName(marker.Value)}')",
                                    severity);
                            }
                        }
                        else if (style == "sublist")
                        {
                            if (lastLevelMarker.TryGetValue(indent, out var expectedMarker))
                            {
                                if (marker != expectedMarker)
                                {
                                    yield return CreateLineViolation(
                                        listItem.Line,
                                        line,
                                        $"Unordered list style should be consistent within same level",
                                        severity);
                                }
                            }
                            lastLevelMarker[indent] = marker.Value;
                        }
                        else
                        {
                            var expectedMarker = GetExpectedMarker(style);
                            if (expectedMarker.HasValue && marker != expectedMarker)
                            {
                                yield return CreateLineViolation(
                                    listItem.Line,
                                    line,
                                    $"Unordered list style should use {GetMarkerName(expectedMarker.Value)}",
                                    severity);
                            }
                        }
                    }
                }
            }
        }

        private char? GetListMarker(string line)
        {
            var trimmed = line.TrimStart();
            if (trimmed.Length > 0 && (trimmed[0] == '*' || trimmed[0] == '-' || trimmed[0] == '+'))
                return trimmed[0];
            return null;
        }

        private int GetIndentLevel(string line)
        {
            var indent = 0;
            foreach (var c in line)
            {
                if (c == ' ') indent++;
                else if (c == '\t') indent += 4;
                else break;
            }
            return indent;
        }

        private char? GetExpectedMarker(string style)
        {
            return style switch
            {
                "asterisk" => '*',
                "plus" => '+',
                "dash" => '-',
                _ => null,
            };
        }

        private string GetMarkerName(char marker)
        {
            return marker switch
            {
                '*' => "asterisk",
                '+' => "plus",
                '-' => "dash",
                _ => marker.ToString(),
            };
        }
    }

    /// <summary>
    /// MD005: Inconsistent indentation for list items at the same level.
    /// </summary>
    public class MD005_ListIndent : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD005");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var levelIndents = new Dictionary<int, int>();

            foreach (ListBlock list in analysis.GetLists())
            {
                // Only analyze top-level lists (nested lists are handled recursively)
                if (list.Parent is ListItemBlock)
                    continue;

                levelIndents.Clear();
                foreach (var violation in AnalyzeList(list, analysis, severity, levelIndents, 0))
                {
                    yield return violation;
                }
            }
        }

        private IEnumerable<LintViolation> AnalyzeList(ListBlock list, MarkdownDocumentAnalysis analysis,
            DiagnosticSeverity severity, Dictionary<int, int> levelIndents, int level)
        {
            foreach (Block item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var line = analysis.GetLine(listItem.Line);
                    var indent = GetIndentLevel(line);

                    if (levelIndents.TryGetValue(level, out var expectedIndent))
                    {
                        if (indent != expectedIndent)
                        {
                            yield return CreateLineViolation(
                                listItem.Line,
                                line,
                                $"Inconsistent indentation for list items at the same level (expected {expectedIndent}, found {indent})",
                                severity);
                        }
                    }
                    else
                    {
                        levelIndents[level] = indent;
                    }

                    // Check nested lists
                    foreach (Block child in listItem)
                    {
                        if (child is ListBlock nestedList)
                        {
                            foreach (LintViolation violation in AnalyzeList(nestedList, analysis, severity, levelIndents, level + 1))
                            {
                                yield return violation;
                            }
                        }
                    }
                }
            }
        }

        private int GetIndentLevel(string line)
        {
            var indent = 0;
            foreach (var c in line)
            {
                if (c == ' ') indent++;
                else if (c == '\t') indent += 4;
                else break;
            }
            return indent;
        }
    }

    /// <summary>
    /// MD007: Unordered list indentation.
    /// </summary>
    public class MD007_UlIndent : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD007");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var indent = configuration.GetIntParameter("indent", 2);

            // Only process top-level lists (not nested lists, which are handled recursively)
            foreach (ListBlock list in analysis.GetLists().Where(l => l.BulletType != '1' && l.Parent is MarkdownDocument))
            {
                foreach (LintViolation violation in AnalyzeList(list, analysis, severity, indent, 0))
                {
                    yield return violation;
                }
            }
        }

        private IEnumerable<LintViolation> AnalyzeList(ListBlock list, MarkdownDocumentAnalysis analysis,
            DiagnosticSeverity severity, int expectedIndent, int level)
        {
            foreach (Block item in list)
            {
                if (item is ListItemBlock listItem)
                {
                    var line = analysis.GetLine(listItem.Line);
                    var actualIndent = GetIndentLevel(line);
                    var expected = level * expectedIndent;

                    if (actualIndent != expected)
                    {
                        yield return CreateLineViolation(
                            listItem.Line,
                            line,
                            $"Unordered list indentation should be {expected} spaces (found {actualIndent})",
                            severity);
                    }

                    foreach (Block child in listItem)
                    {
                        if (child is ListBlock nestedList && nestedList.BulletType != '1')
                        {
                            foreach (LintViolation violation in AnalyzeList(nestedList, analysis, severity, expectedIndent, level + 1))
                            {
                                yield return violation;
                            }
                        }
                    }
                }
            }
        }

        private int GetIndentLevel(string line)
        {
            var indent = 0;
            foreach (var c in line)
            {
                if (c == ' ') indent++;
                else if (c == '\t') indent += 4;
                else break;
            }
            return indent;
        }
    }
}
