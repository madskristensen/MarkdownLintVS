using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MarkdownLintVS.Linting.Rules
{
    /// <summary>
    /// MD033: Inline HTML.
    /// </summary>
    public class MD033_NoInlineHtml : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD033");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var allowedElements = configuration.GetStringParameter("allowed_elements", "")
                .Split(',')
                .Select(e => e.Trim().ToLowerInvariant())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToHashSet();

            foreach (HtmlBlock htmlBlock in analysis.GetHtmlBlocks())
            {
                var line = analysis.GetLine(htmlBlock.Line);
                var element = ExtractElementName(line);

                if (string.IsNullOrEmpty(element) || !allowedElements.Contains(element.ToLowerInvariant()))
                {
                    yield return CreateLineViolation(
                        htmlBlock.Line,
                        line,
                        $"Inline HTML: {element ?? "html"}",
                        severity,
                        "Use Markdown syntax instead");
                }
            }

            foreach (HtmlInline htmlInline in analysis.GetHtmlInlines())
            {
                (int Line, int Column) pos = analysis.GetPositionFromOffset(htmlInline.Span.Start);
                var line = analysis.GetLine(pos.Line);
                var element = ExtractElementName(htmlInline.Tag);

                if (string.IsNullOrEmpty(element) || !allowedElements.Contains(element.ToLowerInvariant()))
                {
                    yield return CreateViolation(
                        pos.Line,
                        pos.Column,
                        pos.Column + htmlInline.Tag.Length,
                        $"Inline HTML: {element ?? "html"}",
                        severity,
                        "Use Markdown syntax instead");
                }
            }
        }

        private string ExtractElementName(string html)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            Match match = Regex.Match(html, @"</?(\w+)");
            return match.Success ? match.Groups[1].Value : null;
        }
    }

    /// <summary>
    /// MD034: Bare URL used.
    /// </summary>
    public class MD034_NoBareUrls : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD034");
        public override RuleInfo Info => _info;

        private static readonly Regex BareUrlPattern = new(
            @"(?<![(<])(https?://[^\s\)>\]]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
                MatchCollection matches = BareUrlPattern.Matches(line);

                foreach (Match match in matches)
                {
                    // Check if URL is inside angle brackets or link syntax
                    var beforeChar = match.Index > 0 ? line[match.Index - 1] : ' ';
                    if (beforeChar == '<' || beforeChar == '(' || beforeChar == '[')
                        continue;

                    yield return CreateViolation(
                        i,
                        match.Index,
                        match.Index + match.Length,
                        "Bare URL used",
                        severity,
                        "Enclose URL in angle brackets or use link syntax");
                }
            }
        }
    }

    /// <summary>
    /// MD035: Horizontal rule style.
    /// </summary>
    public class MD035_HrStyle : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD035");
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

            foreach (ThematicBreakBlock hr in analysis.GetThematicBreaks())
            {
                var line = analysis.GetLine(hr.Line);
                var currentStyle = GetHrStyle(line);

                if (style == "consistent")
                {
                    if (detectedStyle == null)
                    {
                        detectedStyle = currentStyle;
                    }
                    else if (currentStyle != detectedStyle)
                    {
                        yield return CreateLineViolation(
                            hr.Line,
                            line,
                            $"Horizontal rule style should be consistent (expected '{detectedStyle}')",
                            severity);
                    }
                }
                else if (currentStyle != style)
                {
                    yield return CreateLineViolation(
                        hr.Line,
                        line,
                        $"Horizontal rule style should be '{style}'",
                        severity);
                }
            }
        }

        private string GetHrStyle(string line)
        {
            var trimmed = line.Trim();
            if (trimmed.Replace("-", "").Length == 0) return "---";
            if (trimmed.Replace("*", "").Length == 0) return "***";
            if (trimmed.Replace("_", "").Length == 0) return "___";
            return trimmed;
        }
    }

    /// <summary>
    /// MD036: Emphasis used instead of a heading.
    /// </summary>
    public class MD036_NoEmphasisAsHeading : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD036");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            var punctuation = configuration.GetStringParameter("punctuation", ".,;:!?。，；：！？");

            for (var i = 0; i < analysis.LineCount; i++)
            {
                if (analysis.IsLineInCodeBlock(i) || analysis.IsLineInFrontMatter(i))
                    continue;

                // Check if previous and next lines are blank
                var prevBlank = i == 0 || analysis.IsBlankLine(i - 1);
                var nextBlank = i == analysis.LineCount - 1 || analysis.IsBlankLine(i + 1);

                if (!prevBlank || !nextBlank)
                    continue;

                var line = analysis.GetLine(i);
                var trimmed = line.Trim();

                // Check if entire line is emphasis
                if (IsEntireLineEmphasis(trimmed, out var content))
                {
                    // Check if it doesn't end with punctuation
                    if (content.Length > 0 && !punctuation.Contains(content[content.Length - 1]))
                    {
                        yield return CreateLineViolation(
                            i,
                            line,
                            "Emphasis used instead of a heading",
                            severity,
                            "Use heading syntax instead");
                    }
                }
            }
        }

        private bool IsEntireLineEmphasis(string line, out string content)
        {
            content = null;

            // Bold: **text** or __text__
            Match boldMatch = Regex.Match(line, @"^(\*\*|__)(.+)\1$");
            if (boldMatch.Success)
            {
                content = boldMatch.Groups[2].Value;
                return true;
            }

            // Italic: *text* or _text_
            Match italicMatch = Regex.Match(line, @"^(\*|_)([^*_]+)\1$");
            if (italicMatch.Success)
            {
                content = italicMatch.Groups[2].Value;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// MD037: Spaces inside emphasis markers.
    /// </summary>
    public class MD037_NoSpaceInEmphasis : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD037");
        public override RuleInfo Info => _info;

        private static readonly Regex SpaceInEmphasisPattern = new(
            @"(\*+|_+)\s+[^*_]+\s+\1|(\*+|_+)\s+[^*_]+\1|\1[^*_]+\s+(\*+|_+)",
            RegexOptions.Compiled);

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            foreach (EmphasisInline emphasis in analysis.GetEmphasis())
            {
                (int Line, int Column) pos = analysis.GetPositionFromOffset(emphasis.Span.Start);
                var line = analysis.GetLine(pos.Line);
                
                // Get the actual text of the emphasis
                var start = emphasis.Span.Start;
                var end = emphasis.Span.End;
                if (start >= 0 && end < analysis.Text.Length)
                {
                    var text = analysis.Text.Substring(start, end - start + 1);
                    
                    // Check for spaces after opening marker
                    var delimLength = emphasis.DelimiterCount;
                    if (text.Length > delimLength && text[delimLength] == ' ')
                    {
                        yield return CreateViolation(
                            pos.Line,
                            pos.Column,
                            pos.Column + text.Length,
                            "Spaces inside emphasis markers",
                            severity,
                            "Remove space after opening marker");
                    }
                    
                    // Check for spaces before closing marker
                    if (text.Length > delimLength && text[text.Length - delimLength - 1] == ' ')
                    {
                        yield return CreateViolation(
                            pos.Line,
                            pos.Column,
                            pos.Column + text.Length,
                            "Spaces inside emphasis markers",
                            severity,
                            "Remove space before closing marker");
                    }
                }
            }
        }
    }

    /// <summary>
    /// MD038: Spaces inside code span elements.
    /// </summary>
    public class MD038_NoSpaceInCode : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD038");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            foreach (CodeInline codeSpan in analysis.GetCodeSpans())
            {
                var content = codeSpan.Content;
                
                // Single space inside is allowed for escaping backticks
                if (content == " ")
                    continue;
                
                var hasLeadingSpace = content.StartsWith(" ");
                var hasTrailingSpace = content.EndsWith(" ");

                if (hasLeadingSpace || hasTrailingSpace)
                {
                    // Allow if content contains backticks and needs escaping
                    if (content.Contains("`"))
                        continue;

                    (int Line, int Column) pos = analysis.GetPositionFromOffset(codeSpan.Span.Start);
                    yield return CreateViolation(
                        pos.Line,
                        pos.Column,
                        pos.Column + codeSpan.Span.Length,
                        "Spaces inside code span elements",
                        severity,
                        "Remove spaces inside backticks");
                }
            }
        }
    }

    /// <summary>
    /// MD039: Spaces inside link text.
    /// </summary>
    public class MD039_NoSpaceInLinks : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD039");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity)
        {
            foreach (LinkInline link in analysis.GetLinks())
            {
                if (link.FirstChild == null)
                    continue;

                // Get link text
                var linkText = GetLinkText(link);
                
                if (linkText.StartsWith(" ") || linkText.EndsWith(" "))
                {
                    (int Line, int Column) pos = analysis.GetPositionFromOffset(link.Span.Start);
                    yield return CreateViolation(
                        pos.Line,
                        pos.Column,
                        pos.Column + link.Span.Length,
                        "Spaces inside link text",
                        severity,
                        "Remove spaces inside brackets");
                }
            }
        }

        private string GetLinkText(LinkInline link)
        {
            var text = "";
            foreach (Inline child in link)
            {
                if (child is LiteralInline literal)
                    text += literal.Content.ToString();
            }
            return text;
        }
    }
}
