using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class ListRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD004 - Unordered List Style

    [TestMethod]
    public void MD004_WhenConsistentDashStyleThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n- item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenConsistentAsteriskStyleThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("* item 1\n* item 2\n* item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenConsistentPlusStyleThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("+ item 1\n+ item 2\n+ item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenMixedStylesThenReportsViolation()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n* item 2\n+ item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.AreEqual("MD004", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD004_WhenConfiguredDashStyleThenReportsOtherMarkers()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "dash" };
        var analysis = new MarkdownDocumentAnalysis("* item 1\n+ item 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD004_WhenConfiguredAsteriskStyleThenReportsOtherMarkers()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "asterisk" };
        var analysis = new MarkdownDocumentAnalysis("- item 1\n+ item 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD004_WhenStyleDisabledThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "false" };
        var analysis = new MarkdownDocumentAnalysis("- item 1\n* item 2\n+ item 3");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_ViolationMessageContainsExpectedMarkerName()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n* item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("expected 'dash'", violations[0].Message);
        Assert.Contains("found 'asterisk'", violations[0].Message);
    }

    #endregion

    #region MD005 - List Indent

    [TestMethod]
    public void MD005_WhenConsistentIndentationThenNoViolations()
    {
        var rule = new MD005_ListIndent();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n  - nested 1\n  - nested 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD005_WhenInconsistentIndentationThenReportsViolation()
    {
        var rule = new MD005_ListIndent();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n  - nested 1\n   - nested 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD005", violations[0].Rule.Id);
    }

    #endregion

    #region MD029 - Ordered List Prefix

    [TestMethod]
    public void MD029_WhenOrderedPrefixesThenNoViolations()
    {
        var rule = new MD029_OlPrefix();
        var analysis = new MarkdownDocumentAnalysis("1. first\n2. second\n3. third");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD029_WhenAllOnesThenNoViolations()
    {
        var rule = new MD029_OlPrefix();
        var analysis = new MarkdownDocumentAnalysis("1. first\n1. second\n1. third");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD029_WhenConfiguredOneStyleThenReportsOrderedNumbers()
    {
        var rule = new MD029_OlPrefix();
        var config = new RuleConfiguration { Value = "one" };
        var analysis = new MarkdownDocumentAnalysis("1. first\n2. second\n3. third");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD029_WhenConfiguredOrderedStyleThenReportsIncorrectNumbers()
    {
        var rule = new MD029_OlPrefix();
        var config = new RuleConfiguration { Value = "ordered" };
        var analysis = new MarkdownDocumentAnalysis("1. first\n1. second\n1. third");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    #endregion

    #region MD030 - List Marker Space

    [TestMethod]
    public void MD030_WhenSingleSpaceAfterMarkerThenNoViolations()
    {
        var rule = new MD030_ListMarkerSpace();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD030_WhenMultipleSpacesAfterMarkerThenReportsViolation()
    {
        var rule = new MD030_ListMarkerSpace();
        var analysis = new MarkdownDocumentAnalysis("-  item 1\n-  item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.AreEqual("MD030", violations[0].Rule.Id);
    }

    #endregion

    #region MD032 - Blanks Around Lists

    [TestMethod]
    public void MD032_WhenListSurroundedByBlankLinesThenNoViolations()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n- item 1\n- item 2\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD032_WhenNoBlankLineBeforeListThenReportsViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("Some text\n- item 1\n- item 2\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD032", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD032_WhenNoBlankLineAfterListThenReportsViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        // Use a heading after the list to ensure Markdig doesn't treat it as list continuation
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n- item 1\n- item 2\n# Heading");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD032", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD032_WhenListAtStartOfDocumentThenNoViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n\nSome text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD032_WhenListAtEndOfDocumentThenNoViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n- item 1\n- item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion
}
