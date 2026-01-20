using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class HeadingRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD001 - Heading Increment

    [TestMethod]
    public void MD001_WhenHeadingsIncrementByOneThenNoViolations()
    {
        var rule = new MD001_HeadingIncrement();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1\n\n## Heading 2\n\n### Heading 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD001_WhenHeadingSkipsLevelThenReportsViolation()
    {
        var rule = new MD001_HeadingIncrement();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1\n\n### Heading 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD001", violations[0].Rule.Id);
        Assert.Contains("expected h2", violations[0].Message);
    }

    [TestMethod]
    public void MD001_WhenHeadingDecrementsThenNoViolation()
    {
        var rule = new MD001_HeadingIncrement();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1\n\n## Heading 2\n\n# Another H1");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD001_WhenMultipleSkipsThenReportsAllViolations()
    {
        var rule = new MD001_HeadingIncrement();
        var analysis = new MarkdownDocumentAnalysis("# H1\n\n### H3\n\n###### H6");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD001_WhenNoHeadingsThenNoViolations()
    {
        var rule = new MD001_HeadingIncrement();
        var analysis = new MarkdownDocumentAnalysis("Just some text\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD003 - Heading Style

    [TestMethod]
    public void MD003_WhenConsistentAtxStyleThenNoViolations()
    {
        var rule = new MD003_HeadingStyle();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1\n\n## Heading 2\n\n### Heading 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD003_WhenMixedStylesThenReportsViolation()
    {
        var rule = new MD003_HeadingStyle();
        var analysis = new MarkdownDocumentAnalysis("# ATX Heading\n\nSetext Heading\n=============");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD003", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD003_WhenConfiguredAtxStyleThenReportsSetextViolation()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "atx" };
        var analysis = new MarkdownDocumentAnalysis("Setext Heading\n=============");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD003_WhenStyleDisabledThenNoViolations()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "false" };
        var analysis = new MarkdownDocumentAnalysis("# ATX\n\nSetext\n======");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD003_WhenAtxClosedStyleThenDetected()
    {
        var rule = new MD003_HeadingStyle();
        var analysis = new MarkdownDocumentAnalysis("# Closed Heading #\n\n## Another Closed ##");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion
}
