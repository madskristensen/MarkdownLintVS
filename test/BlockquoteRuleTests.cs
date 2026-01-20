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

    #endregion
}
