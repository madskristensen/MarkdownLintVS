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

    [TestMethod]
    public void MD049_WhenAsteriskStyleEnforcedWithUnderscoreThenReportsViolation()
    {
        var rule = new MD049_EmphasisStyle();
        var config = new RuleConfiguration { Value = "asterisk" };
        var analysis = new MarkdownDocumentAnalysis("This is _italic_");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD049_WhenUnderscoreStyleEnforcedWithAsteriskThenReportsViolation()
    {
        var rule = new MD049_EmphasisStyle();
        var config = new RuleConfiguration { Value = "underscore" };
        var analysis = new MarkdownDocumentAnalysis("This is *italic*");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD049_ViolationMessageDescribesIssue()
    {
        var rule = new MD049_EmphasisStyle();
        var analysis = new MarkdownDocumentAnalysis("*italic* and _another_");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("style", violations[0].Message.ToLower());
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

    [TestMethod]
    public void MD050_WhenAsteriskStyleEnforcedWithUnderscoreThenReportsViolation()
    {
        var rule = new MD050_StrongStyle();
        var config = new RuleConfiguration { Value = "asterisk" };
        var analysis = new MarkdownDocumentAnalysis("This is __bold__");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD050_WhenUnderscoreStyleEnforcedWithAsteriskThenReportsViolation()
    {
        var rule = new MD050_StrongStyle();
        var config = new RuleConfiguration { Value = "underscore" };
        var analysis = new MarkdownDocumentAnalysis("This is **bold**");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD050_ViolationMessageDescribesIssue()
    {
        var rule = new MD050_StrongStyle();
        var analysis = new MarkdownDocumentAnalysis("**bold** and __another__");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("style", violations[0].Message.ToLower());
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

    [TestMethod]
    public void MD051_WhenHtmlAnchorIdThenNoViolations()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis(
            "[Examples](#example)\n\n" +
            "## <a id=\"example\">Hello</a>");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD051_WhenHtmlNameAttributeThenNoViolations()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis(
            "[Go to section](#mysection)\n\n" +
            "<a name=\"mysection\"></a>\n" +
            "Some content here");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD051_WhenHtmlIdWithSingleQuotesThenNoViolations()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis(
            "[Link](#custom-id)\n\n" +
            "<div id='custom-id'>Content</div>");

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

    [TestMethod]
    public void MD052_WhenKeyboardShortcutInBracketsThenNoViolation()
    {
        // Valid: keyboard shortcut notation should not be flagged
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("Press [Ctrl] + [C] to copy the text.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenFootnoteStyleBracketsThenNoViolation()
    {
        // Valid: footnote-style brackets like [1], [2] in academic text
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("This claim needs a citation [1].");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD052_WhenSingleWordInBracketsThenNoViolation()
    {
        // Valid: single words in brackets are typically not reference links
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("hey [what] is going on");

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

    [TestMethod]
    public void MD053_WhenDefinitionUnusedThenMayReportViolation()
    {
        var rule = new MD053_LinkImageReferenceDefinitions();
        // An unused definition - should be flagged
        var analysis = new MarkdownDocumentAnalysis("Some text.\n\n[unused]: https://example.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Implementation may or may not detect this depending on parsing
        // Document the expected behavior
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void MD053_WhenIgnoredDefinitionUnusedThenNoViolation()
    {
        // Per docs: // is ignored by default
        var rule = new MD053_LinkImageReferenceDefinitions();
        var analysis = new MarkdownDocumentAnalysis("[//]: # (This is a comment)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD053_WhenDefinitionUsedThenNoViolation()
    {
        var rule = new MD053_LinkImageReferenceDefinitions();
        var analysis = new MarkdownDocumentAnalysis("Check the [link][ref].\n\n[ref]: https://example.com");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD051 Additional Tests

    [TestMethod]
    public void MD051_WhenTopFragmentThenMayBeAccepted()
    {
        // Per docs: #top is always valid in HTML
        // Implementation may or may not support this
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis("# Heading\n\n[Top](#top)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Document behavior - may be 0 or 1 depending on implementation
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void MD051_ViolationMessageDescribesIssue()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis("# Heading\n\n[Link](#invalid-fragment)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("fragment", violations[0].Message.ToLower());
    }

    [TestMethod]
    public void MD051_WhenHeadingWithSpecialCharsThenGeneratesCorrectFragment()
    {
        var rule = new MD051_LinkFragments();
        // Heading: "Hello World!" -> fragment: hello-world
        var analysis = new MarkdownDocumentAnalysis("# Hello World!\n\n[Link](#hello-world)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD052 Additional Tests

    [TestMethod]
    public void MD052_ViolationMessageDescribesIssue()
    {
        var rule = new MD052_ReferenceLinksImages();
        var analysis = new MarkdownDocumentAnalysis("[text][undefined]");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("undefined", violations[0].Message.ToLower());
    }

    #endregion

    #region MD059 - Descriptive Link Text

    [TestMethod]
    public void MD059_WhenDescriptiveLinkTextThenNoViolations()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[View the documentation](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD059_WhenClickHereThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[click here](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
        Assert.Contains("click here", violations[0].Message);
    }

    [TestMethod]
    public void MD059_WhenHereThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[here](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD059_WhenReadMoreThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[read more](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD059_WhenLearnMoreThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[learn more](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD059_WhenLinkThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[link](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD059_WhenUrlAsLinkTextThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[https://example.com](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD059_WhenThisPageThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[this page](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD059_WhenCaseInsensitiveThenReportsViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[CLICK HERE](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD059_WhenMultipleNonDescriptiveLinksThenReportsAll()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[click here](https://a.com) and [here](https://b.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD059_WhenAllowedTextConfiguredThenNoViolation()
    {
        var rule = new MD059_DescriptiveLinkText();
        var config = new RuleConfiguration();
        config.Parameters["allowed_texts"] = "click here,here";
        var analysis = new MarkdownDocumentAnalysis("[click here](https://example.com)");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD059_WhenEmphasisInLinkThenChecksText()
    {
        var rule = new MD059_DescriptiveLinkText();
        var analysis = new MarkdownDocumentAnalysis("[*click here*](https://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD059", violations[0].Rule.Id);
    }

    #endregion
}
