using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class BlockquoteRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD027 - No Multiple Space Blockquote

    [TestMethod]
    public void MD027_WhenSingleSpaceAfterBlockquoteThenNoViolations()
    {
        var rule = new MD027_NoMultipleSpaceBlockquote();
        var analysis = new MarkdownDocumentAnalysis("> quoted text\n> more quoted text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD027_WhenMultipleSpacesAfterBlockquoteThenReportsViolation()
    {
        var rule = new MD027_NoMultipleSpaceBlockquote();
        var analysis = new MarkdownDocumentAnalysis(">  quoted text with extra space");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD027", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD027_WhenNestedBlockquoteWithMultipleSpacesThenReportsViolation()
    {
        var rule = new MD027_NoMultipleSpaceBlockquote();
        var analysis = new MarkdownDocumentAnalysis(">>  nested with extra space");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD027_WhenInCodeBlockThenSkipped()
    {
        var rule = new MD027_NoMultipleSpaceBlockquote();
        var analysis = new MarkdownDocumentAnalysis("```\n>  not a real blockquote\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD028 - No Blanks Blockquote

    [TestMethod]
    public void MD028_WhenContinuousBlockquoteThenNoViolations()
    {
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis("> line 1\n> line 2\n> line 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD028_WhenBlankLineInsideBlockquoteThenReportsViolation()
    {
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis("> line 1\n\n> line 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD028", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD028_WhenSeparateBlockquotesThenNoViolation()
    {
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis("> quote 1\n\nSome text\n\n> quote 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD028_WhenBlockquoteWithBlankPrefixLineThenNoViolation()
    {
        // Per docs: blockquote symbol at beginning of blank line keeps it same quote
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis("> line 1\n>\n> line 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD028_ViolationMessageDescribesIssue()
    {
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis("> quote 1\n\n> quote 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Blank line", violations[0].Message);
    }

    [TestMethod]
    public void MD028_WhenGitHubAlertsThenNoViolation()
    {
        // GitHub alerts are semantically distinct blocks and should be separated by blank lines
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis(
            "> [!IMPORTANT]\n> Crucial information.\n\n> [!WARNING]\n> Critical content.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD028_WhenAlertFollowsRegularBlockquoteThenNoViolation()
    {
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis(
            "> Regular quote.\n\n> [!NOTE]\n> This is a note.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD028_WhenRegularBlockquoteFollowsAlertThenNoViolation()
    {
        var rule = new MD028_NoBlanksBlockquote();
        var analysis = new MarkdownDocumentAnalysis(
            "> [!TIP]\n> This is a tip.\n\n> Regular quote.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD027 Additional Tests

    [TestMethod]
    public void MD027_WhenListItemsDisabledThenSkipsListItems()
    {
        var rule = new MD027_NoMultipleSpaceBlockquote();
        var config = new RuleConfiguration();
        config.Parameters["list_items"] = "false";
        var analysis = new MarkdownDocumentAnalysis(">  - list item with extra space");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // With list_items=false, should skip list items
        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD027_ViolationMessageDescribesIssue()
    {
        var rule = new MD027_NoMultipleSpaceBlockquote();
        var analysis = new MarkdownDocumentAnalysis(">  extra space");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Multiple spaces", violations[0].Message);
    }

    #endregion
}
