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
}
