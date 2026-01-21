using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class HeadingRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    [TestMethod]
    public void MD003_WhenFrontMatterBeforeHeadingsThenNoViolations()
    {
        var rule = new MD003_HeadingStyle();
        var markdown =
            "---\n" +
            "description: 'Test'\n" +
            "applyTo: '**/*.cs'\n" +
            "---\n" +
            "\n" +
            "# Title\n\n" +
            "## Scope\n";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsFalse(violations.Any(v => v.Message.Contains("preceded by")));
    }

    [TestMethod]
    public void MD022_WhenFrontMatterBeforeHeadingThenNoViolationForBlankLinesAbove()
    {
        var rule = new MD022_BlanksAroundHeadings();
        var markdown =
            "---\n" +
            "description: 'Test'\n" +
            "applyTo: '**/*.cs'\n" +
            "---\n" +
            "\n" +
            "# Title\n\n" +
            "Text\n";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsFalse(violations.Any(v => v.Message.Contains("preceded by")));
    }

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
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD001"));
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
        Assert.AreEqual("MD003", violations[0].Rule.Id);
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

    [TestMethod]
    public void MD003_WhenConsistentModeSetextWithAtxH3ThenAllowed()
    {
        // In consistent mode, setext_with_atx behavior allows ATX H3+ when first headings are setext
        var rule = new MD003_HeadingStyle();
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "Setext H2\n" +
            "---------\n\n" +
            "### ATX H3";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // The implementation allows ATX H3+ when setext is detected for H1/H2 in consistent mode
        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD003_WhenSetextWithAtxStyleAllowsAtxH3()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "setext_with_atx" };
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "Setext H2\n" +
            "---------\n\n" +
            "### ATX H3";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD003_WhenSetextWithAtxStyleReportsAtxH1H2()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "setext_with_atx" };
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "## ATX H2";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // ATX H2 should be reported when setext_with_atx is configured
        Assert.HasCount(1, violations);
        Assert.AreEqual("MD003", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD003_WhenSetextWithAtxClosedStyleAllowsClosedAtxH3()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "setext_with_atx_closed" };
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "### ATX Closed H3 ###";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD003_WhenSetextWithAtxClosedStyleReportsOpenAtxH3()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "setext_with_atx_closed" };
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "### Open ATX H3";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // Open ATX H3 should be reported when setext_with_atx_closed is configured
        Assert.HasCount(1, violations);
        Assert.Contains("atx_closed", violations[0].Message);
    }

    [TestMethod]
    public void MD003_WhenConfiguredAtxStyleReportsSetext()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "atx" };
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "## ATX H2";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // Setext heading should be reported as non-atx
        Assert.HasCount(1, violations);
        Assert.AreEqual("MD003", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD003_WhenConfiguredAtxClosedStyleReportsOpenAtx()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "atx_closed" };
        var markdown =
            "# Closed H1 #\n\n" +
            "## Open ATX H2";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD003", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD003_WhenPureSetextStyleConfiguredReportsAtx()
    {
        var rule = new MD003_HeadingStyle();
        var config = new RuleConfiguration { Value = "setext" };
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "### ATX H3";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD003", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD003_WhenConsistentSetextStyleThenNoViolations()
    {
        var rule = new MD003_HeadingStyle();
        var markdown =
            "Setext H1\n" +
            "=========\n\n" +
            "Setext H2\n" +
            "---------";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD003_WhenHorizontalRuleCreatesAccidentalSetextThenDetected()
    {
        var rule = new MD003_HeadingStyle();
        // Text followed by --- can become a setext heading
        var markdown =
            "# ATX H1\n\n" +
            "Some text that becomes a heading\n" +
            "---";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Should detect the mixed styles (atx vs setext)
        Assert.HasCount(1, violations);
    }

    #endregion

    #region MD001 - Additional Tests

    [TestMethod]
    public void MD001_WhenFrontMatterWithTitleThenFirstHeadingMustBeH2()
    {
        var rule = new MD001_HeadingIncrement();
        var markdown =
            "---\n" +
            "title: My Document\n" +
            "---\n\n" +
            "### H3 As First Heading";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Front matter title acts as H1, so H3 skips H2
        Assert.HasCount(1, violations);
        Assert.Contains("expected h2", violations[0].Message);
    }

    [TestMethod]
    public void MD001_WhenFrontMatterWithTitleAndH2FirstThenNoViolation()
    {
        var rule = new MD001_HeadingIncrement();
        var markdown =
            "---\n" +
            "title: My Document\n" +
            "---\n\n" +
            "## H2 As First Heading\n\n" +
            "### H3";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD001_WhenFrontMatterTitleDisabledThenFirstHeadingCanBeAny()
    {
        var rule = new MD001_HeadingIncrement();
        var config = new RuleConfiguration();
        config.Parameters["front_matter_title"] = "";  // Empty string disables the feature

        var markdown =
            "---\n" +
            "title: My Document\n" +
            "---\n\n" +
            "### H3 As First Heading";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // Disabled front_matter_title means first heading can be any level
        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD001_WhenFrontMatterWithCustomTitlePatternThenMatches()
    {
        var rule = new MD001_HeadingIncrement();
        var config = new RuleConfiguration();
        config.Parameters["front_matter_title"] = @"^\s*name\s*:";  // Custom pattern

        var markdown =
            "---\n" +
            "name: My Document\n" +
            "---\n\n" +
            "### H3 As First Heading";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // Custom pattern matches "name:", so H3 skips H2
        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD001_WhenFrontMatterWithoutTitleThenFirstHeadingCanBeAny()
    {
        var rule = new MD001_HeadingIncrement();
        var markdown =
            "---\n" +
            "description: No title here\n" +
            "---\n\n" +
            "### H3 As First Heading";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // No title in front matter, so first heading can be any level
        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD001_WhenDecrementingAndReincreasingThenNoViolation()
    {
        var rule = new MD001_HeadingIncrement();
        var markdown =
            "# H1\n\n" +
            "## H2\n\n" +
            "### H3\n\n" +
            "#### H4\n\n" +
            "## Another H2\n\n" +
            "### Another H3";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD001_WhenStartingAtH2ThenNoViolation()
    {
        var rule = new MD001_HeadingIncrement();
        // First heading can be any level when no front matter
        var analysis = new MarkdownDocumentAnalysis("## H2\n\n### H3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD001_WhenSetextHeadingsSkipLevelThenReportsViolation()
    {
        var rule = new MD001_HeadingIncrement();
        var markdown =
            "H1 Setext\n" +
            "=========\n\n" +
            "#### H4 ATX";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Skipping from H1 to H4
        Assert.HasCount(1, violations);
        Assert.Contains("expected h2", violations[0].Message);
    }

    #endregion
}
