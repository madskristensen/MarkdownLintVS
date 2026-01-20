using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class LinkRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD049 - Emphasis Style

    [TestMethod]
    public void MD049_WhenConsistentAsteriskStyleThenNoViolations()
    {
        var rule = new MD049_EmphasisStyle();
        var analysis = new MarkdownDocumentAnalysis("This is *italic* and *another italic*");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD049_WhenConsistentUnderscoreStyleThenNoViolations()
    {
        var rule = new MD049_EmphasisStyle();
        var analysis = new MarkdownDocumentAnalysis("This is _italic_ and _another italic_");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD049_WhenMixedStylesThenReportsViolation()
    {
        var rule = new MD049_EmphasisStyle();
        var analysis = new MarkdownDocumentAnalysis("This is *italic* and _another italic_");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD049", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD049_WhenStyleDisabledThenNoViolations()
    {
        var rule = new MD049_EmphasisStyle();
        var config = new RuleConfiguration { Value = "false" };
        var analysis = new MarkdownDocumentAnalysis("This is *italic* and _another italic_");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD050 - Strong Style

    [TestMethod]
    public void MD050_WhenConsistentAsteriskStyleThenNoViolations()
    {
        var rule = new MD050_StrongStyle();
        var analysis = new MarkdownDocumentAnalysis("This is **bold** and **another bold**");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD050_WhenConsistentUnderscoreStyleThenNoViolations()
    {
        var rule = new MD050_StrongStyle();
        var analysis = new MarkdownDocumentAnalysis("This is __bold__ and __another bold__");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD050_WhenMixedStylesThenReportsViolation()
    {
        var rule = new MD050_StrongStyle();
        var analysis = new MarkdownDocumentAnalysis("This is **bold** and __another bold__");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD050", violations[0].Rule.Id);
    }

    #endregion

    #region MD051 - Link Fragments

    [TestMethod]
    public void MD051_WhenValidFragmentLinkThenNoViolations()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis("# My Heading\n\n[link](#my-heading)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD051_WhenInvalidFragmentLinkThenReportsViolation()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis("# My Heading\n\n[link](#non-existent)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD051", violations[0].Rule.Id);
        Assert.Contains("non-existent", violations[0].Message);
    }

    [TestMethod]
    public void MD051_WhenExternalLinkThenNoViolations()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis("[link](https://example.com#section)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD052 - Reference Links Images

    [TestMethod]
    public void MD052_WhenValidReferenceLinksDefinedThenNoViolations()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("[text][label]\n\n[label]: https://example.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenReferenceLabelNotDefinedThenReportsViolation()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("[text][undefined-label]");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD052", violations[0].Rule.Id);
        Assert.Contains("undefined-label", violations[0].Message);
    }

    [TestMethod]
    public void MD052_WhenTaskListCheckboxUncheckedThenNoViolation()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("- [ ] unchecked task item\n- [ ] another task");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenTaskListCheckboxCheckedLowercaseThenNoViolation()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("- [x] checked task item\n- [x] another task");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenTaskListCheckboxCheckedUppercaseThenNoViolation()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("- [X] checked task item\n- [X] another task");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenMixedTaskListAndReferencesThenOnlyReportsUndefinedReferences()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis(
            "- [ ] task item\n" +
            "- [x] checked item\n" +
            "[valid link][defined]\n" +
            "[invalid link][undefined]\n\n" +
            "[defined]: https://example.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("undefined", violations[0].Message);
    }

    [TestMethod]
    public void MD052_WhenShortcutSyntaxDisabledThenSkipsShortcutLinks()
    {
        var rule = new MD052_ReferenceLinksImages();
        var config = new RuleConfiguration();
        config.Parameters["shortcut_syntax"] = "false";
        var analysis = new MarkdownDocumentAnalysis("[shortcut-label]");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenCollapsedReferenceThenChecksLabel()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("[label][]\n\n[label]: https://example.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenInCodeBlockThenSkipped()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("```\n[undefined-ref]\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD053 - Link Image Reference Definitions

    [TestMethod]
    public void MD053_WhenAllDefinitionsUsedThenNoViolations()
    {
        var rule = new MD053_LinkImageReferenceDefinitions();
        var analysis = new MarkdownDocumentAnalysis("[link][label]\n\n[label]: https://example.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion
}
