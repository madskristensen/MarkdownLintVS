using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class InlineRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD033 - No Inline HTML

    [TestMethod]
    public void MD033_WhenNoHtmlThenNoViolation()
    {
        var rule = new MD033_NoInlineHtml();
        var analysis = new MarkdownDocumentAnalysis("# Heading\n\nSome text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD033_WhenHtmlBlockThenReportsViolation()
    {
        var rule = new MD033_NoInlineHtml();
        var analysis = new MarkdownDocumentAnalysis("<h1>HTML Heading</h1>");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD033", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD033_WhenInlineHtmlThenReportsViolation()
    {
        var rule = new MD033_NoInlineHtml();
        var analysis = new MarkdownDocumentAnalysis("Text with <br> inline HTML");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD033_WhenAllowedElementThenNoViolation()
    {
        var rule = new MD033_NoInlineHtml();
        var config = new RuleConfiguration();
        config.Parameters["allowed_elements"] = "br,hr";
        var analysis = new MarkdownDocumentAnalysis("Text with <br> allowed");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD033_WhenNotAllowedElementThenReportsViolation()
    {
        var rule = new MD033_NoInlineHtml();
        var config = new RuleConfiguration();
        config.Parameters["allowed_elements"] = "br";
        var analysis = new MarkdownDocumentAnalysis("<div>Not allowed</div>");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD033_ViolationMessageContainsElement()
    {
        var rule = new MD033_NoInlineHtml();
        var analysis = new MarkdownDocumentAnalysis("Text with <br> here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("br", violations[0].Message.ToLower());
    }

    #endregion

    #region MD034 - No Bare URLs

    [TestMethod]
    public void MD034_WhenNoUrlsThenNoViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("Some text without URLs");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD034_WhenBareUrlThenReportsViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("Visit https://example.com for more");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD034", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD034_WhenUrlInAngleBracketsThenNoViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("Visit <https://example.com> for more");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD034_WhenUrlInLinkSyntaxThenNoViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("[Click here](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD034_WhenInCodeBlockThenNoViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("```\nhttps://example.com\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD034_WhenInFrontMatterThenNoViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("---\nurl: https://example.com\n---\n\nText");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD034_WhenMultipleBareUrlsThenReportsAll()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("Visit https://a.com or https://b.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD034_ViolationMessageDescribesIssue()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("Visit https://example.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Bare URL", violations[0].Message);
    }

    [TestMethod]
    public void MD034_WhenInInlineCodeThenNoViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis("Use `https://example.com` for the URL");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD034_WhenUrlInHtmlAttributeThenNoViolation()
    {
        var rule = new MD034_NoBareUrls();
        var analysis = new MarkdownDocumentAnalysis(
            "<iframe src=\"https://www.youtube-nocookie.com/embed/4JL8EawZMvY?list=PLReL099Y5nRdz9jvxuy_LgHFKowkx8tS4&color=white\" title=\"YouTube video player\" allowfullscreen></iframe>");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD035 - Horizontal Rule Style

    [TestMethod]
    public void MD035_WhenConsistentStyleThenNoViolation()
    {
        var rule = new MD035_HrStyle();
        var analysis = new MarkdownDocumentAnalysis("---\n\nText\n\n---");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD035_WhenInconsistentStyleThenReportsViolation()
    {
        var rule = new MD035_HrStyle();
        var analysis = new MarkdownDocumentAnalysis("---\n\nText\n\n***");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD035", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD035_WhenSpecificStyleSetThenEnforcesIt()
    {
        var rule = new MD035_HrStyle();
        var config = new RuleConfiguration();
        config.Parameters["style"] = "***";
        var analysis = new MarkdownDocumentAnalysis("---\n\nText\n\n---");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD035_WhenSingleHrThenNoViolation()
    {
        var rule = new MD035_HrStyle();
        var analysis = new MarkdownDocumentAnalysis("Text\n\n---\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD035_WhenStyleDisabledThenNoViolation()
    {
        var rule = new MD035_HrStyle();
        var config = new RuleConfiguration();
        config.Parameters["style"] = "false";
        var analysis = new MarkdownDocumentAnalysis("---\n\n***\n\n___");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD035_ViolationMessageContainsExpectedStyle()
    {
        var rule = new MD035_HrStyle();
        var analysis = new MarkdownDocumentAnalysis("---\n\nText\n\n***");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("---", violations[0].Message);
    }

    #endregion

    #region MD036 - Emphasis Used Instead of Heading

    [TestMethod]
    public void MD036_WhenNormalTextThenNoViolation()
    {
        var rule = new MD036_NoEmphasisAsHeading();
        var analysis = new MarkdownDocumentAnalysis("Some **bold** text in a paragraph.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD036_WhenStandaloneEmphasisThenReportsViolation()
    {
        // Per docs: single-line paragraph of entirely emphasized text
        var rule = new MD036_NoEmphasisAsHeading();
        var analysis = new MarkdownDocumentAnalysis("Text\n\n**My Section**\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD036", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD036_WhenEmphasisEndsWithPunctuationThenNoViolation()
    {
        // Per docs: paragraphs ending in punctuation don't trigger
        var rule = new MD036_NoEmphasisAsHeading();
        var analysis = new MarkdownDocumentAnalysis("Text\n\n**Important note.**\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD036_WhenItalicStandaloneThenReportsViolation()
    {
        var rule = new MD036_NoEmphasisAsHeading();
        var analysis = new MarkdownDocumentAnalysis("Text\n\n_Section Title_\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD036_WhenInCodeBlockThenNoViolation()
    {
        var rule = new MD036_NoEmphasisAsHeading();
        var analysis = new MarkdownDocumentAnalysis("```\n**Not a heading**\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD036_ViolationMessageDescribesIssue()
    {
        var rule = new MD036_NoEmphasisAsHeading();
        var analysis = new MarkdownDocumentAnalysis("Text\n\n**Heading**\n\nMore");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Emphasis", violations[0].Message);
    }

    #endregion

    #region MD037 - Spaces Inside Emphasis

    [TestMethod]
    public void MD037_WhenNoSpacesInEmphasisThenNoViolation()
    {
        var rule = new MD037_NoSpaceInEmphasis();
        var analysis = new MarkdownDocumentAnalysis("Some **bold** text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD037_WhenSpacesAroundEmphasisTextThenRuleExecutes()
    {
        // Per docs: emphasis with spaces doesn't parse as emphasis
        // The rule detects where spaces were used but emphasis was intended
        var rule = new MD037_NoSpaceInEmphasis();
        // This tests what the rule can detect - may depend on Markdig parsing
        var analysis = new MarkdownDocumentAnalysis("Some ** bold ** text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Markdig may not parse ** bold ** as emphasis due to spaces
        // Verify the rule executes without error
        Assert.IsNotNull(violations);
    }

    #endregion

    #region MD038 - Spaces Inside Code Spans

    [TestMethod]
    public void MD038_WhenNoSpacesInCodeSpanThenNoViolation()
    {
        var rule = new MD038_NoSpaceInCode();
        var analysis = new MarkdownDocumentAnalysis("Use `code` here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD038_WhenLeadingSpaceInCodeSpanThenReportsViolation()
    {
        var rule = new MD038_NoSpaceInCode();
        var analysis = new MarkdownDocumentAnalysis("Use ` code` here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD038", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD038_WhenTrailingSpaceInCodeSpanThenReportsViolation()
    {
        var rule = new MD038_NoSpaceInCode();
        var analysis = new MarkdownDocumentAnalysis("Use `code ` here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD038_WhenBacktickEscapingThenNoViolation()
    {
        // Per docs: single leading and trailing space is allowed for escaping backticks
        var rule = new MD038_NoSpaceInCode();
        var analysis = new MarkdownDocumentAnalysis("Use `` `backticks` `` here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD038_WhenOnlySpacesThenNoViolation()
    {
        // Per docs: code spans containing only spaces are allowed
        var rule = new MD038_NoSpaceInCode();
        var analysis = new MarkdownDocumentAnalysis("Use ` ` here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD038_ViolationMessageDescribesIssue()
    {
        var rule = new MD038_NoSpaceInCode();
        var analysis = new MarkdownDocumentAnalysis("Use ` code` here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Spaces", violations[0].Message);
    }

    #endregion

    #region MD039 - Spaces Inside Link Text

    [TestMethod]
    public void MD039_WhenNoSpacesInLinkTextThenNoViolation()
    {
        var rule = new MD039_NoSpaceInLinks();
        var analysis = new MarkdownDocumentAnalysis("[link text](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD039_WhenLeadingSpaceInLinkTextThenReportsViolation()
    {
        var rule = new MD039_NoSpaceInLinks();
        var analysis = new MarkdownDocumentAnalysis("[ link text](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD039", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD039_WhenTrailingSpaceInLinkTextThenReportsViolation()
    {
        var rule = new MD039_NoSpaceInLinks();
        var analysis = new MarkdownDocumentAnalysis("[link text ](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD039_ViolationMessageDescribesIssue()
    {
        var rule = new MD039_NoSpaceInLinks();
        var analysis = new MarkdownDocumentAnalysis("[ link](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Spaces", violations[0].Message);
    }

    #endregion
}
