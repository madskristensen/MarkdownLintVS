using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class WhitespaceRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD009 - Trailing Spaces

    [TestMethod]
    public void MD009_WhenNoTrailingSpacesThenNoViolations()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line without trailing spaces\nAnother line");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_WhenTrailingSpacesThenReportsViolation()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line with trailing spaces   ");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD009", violations[0].Rule.Id);
        Assert.AreEqual(0, violations[0].LineNumber);
    }

    [TestMethod]
    public void MD009_WhenTwoSpacesForLineBreakThenNoViolationByDefault()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line with two trailing spaces  ");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_WhenStrictModeThenReportsTwoSpaces()
    {
        var rule = new MD009_NoTrailingSpaces();
        var config = new RuleConfiguration();
        config.Parameters["strict"] = "true";
        var analysis = new MarkdownDocumentAnalysis("Line with two trailing spaces  ");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD009_WhenInCodeBlockThenSkipped()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("```\ncode with spaces   \n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD010 - Hard Tabs

    [TestMethod]
    public void MD010_WhenNoTabsThenNoViolations()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("No tabs here\n    Spaces only");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD010_WhenTabPresentThenReportsViolation()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("Line\twith tab");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD010", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD010_WhenMultipleTabsThenReportsEachViolation()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("Line\twith\ttabs");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD010_WhenCodeBlocksDisabledThenSkipsCodeBlocks()
    {
        var rule = new MD010_NoHardTabs();
        var config = new RuleConfiguration();
        config.Parameters["code_blocks"] = "false";
        var analysis = new MarkdownDocumentAnalysis("```\ncode\twith\ttab\n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD011 - Reversed Links

    [TestMethod]
    public void MD011_WhenCorrectLinkSyntaxThenNoViolations()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("[Link text](http://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenReversedLinkSyntaxThenReportsViolation()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("(http://example.com)[Link text]");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD011", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD011_WhenInCodeBlockThenSkipped()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("```\n(http://example.com)[Link text]\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion
}
