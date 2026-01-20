using Markdig.Extensions.Tables;
using Markdig.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace MarkdownLintVS.Linting.Rules
{
    /// <summary>
    /// MD055: Table pipe style.
    /// </summary>
    public class MD055_TablePipeStyle : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD055");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var style = configuration.GetStringParameter("style", "consistent");
            if (style == "false")
                yield break;

            string detectedStyle = null;

            foreach (Table table in analysis.GetTables())
            {
                for (var rowIndex = 0; rowIndex < table.Count; rowIndex++)
                {
                    Block row = table[rowIndex];
                    if (row is TableRow tableRow)
                    {
                        var lineNum = tableRow.Line;
                        var line = analysis.GetLine(lineNum);
                        var currentStyle = GetPipeStyle(line);

                        if (style == "consistent")
                        {
                            if (detectedStyle == null)
                            {
                                detectedStyle = currentStyle;
                            }
                            else if (currentStyle != detectedStyle)
                            {
                                yield return CreateLineViolation(
                                    lineNum,
                                    line,
                                    $"Table pipe style should be consistent (expected {detectedStyle})",
                                    severity);
                            }
                        }
                        else if (currentStyle != style)
                        {
                            yield return CreateLineViolation(
                                lineNum,
                                line,
                                $"Table pipe style should be {style}",
                                severity);
                        }
                    }
                }
            }
        }

        private string GetPipeStyle(string line)
        {
            var trimmed = line.Trim();
            var hasLeading = trimmed.StartsWith("|");
            var hasTrailing = trimmed.EndsWith("|");

            if (hasLeading && hasTrailing) return "leading_and_trailing";
            if (hasLeading) return "leading_only";
            if (hasTrailing) return "trailing_only";
            return "no_leading_or_trailing";
        }
    }

    /// <summary>
    /// MD056: Table column count.
    /// </summary>
    public class MD056_TableColumnCount : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD056");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            foreach (Table table in analysis.GetTables())
            {
                int? expectedColumns = null;
                var headerLine = -1;

                foreach (Block row in table)
                {
                    if (row is TableRow tableRow)
                    {
                        var columnCount = tableRow.Count;
                        var lineNum = tableRow.Line;

                        if (expectedColumns == null)
                        {
                            expectedColumns = columnCount;
                            headerLine = lineNum;
                        }
                        else if (columnCount != expectedColumns)
                        {
                            yield return CreateLineViolation(
                                lineNum,
                                analysis.GetLine(lineNum),
                                $"Table column count should be {expectedColumns} (found {columnCount})",
                                severity);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// MD058: Tables should be surrounded by blank lines.
    /// </summary>
    public class MD058_BlanksAroundTables : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD058");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            foreach (Table table in analysis.GetTables())
            {
                var startLine = table.Line;
                var endLine = analysis.GetBlockEndLine(table);

                // Check line before
                if (startLine > 0 && !analysis.IsBlankLine(startLine - 1))
                {
                    yield return CreateLineViolation(
                        startLine,
                        analysis.GetLine(startLine),
                        "Tables should be surrounded by blank lines",
                        severity,
                        "Add blank line before table");
                }

                // Check line after
                if (endLine < analysis.LineCount - 1 && !analysis.IsBlankLine(endLine + 1))
                {
                    yield return CreateLineViolation(
                        endLine,
                        analysis.GetLine(endLine),
                        "Tables should be surrounded by blank lines",
                        severity,
                        "Add blank line after table");
                }
            }
        }
    }
}
