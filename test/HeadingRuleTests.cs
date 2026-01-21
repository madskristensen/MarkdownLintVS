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

    #region MD018 - No Space After Hash

    [TestMethod]
    public void MD018_WhenSpaceAfterHashThenNoViolation()
    {
        var rule = new MD018_NoMissingSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1\n\n## Heading 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD018_WhenNoSpaceAfterHashThenReportsViolation()
    {
        var rule = new MD018_NoMissingSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("#Heading 1\n\n##Heading 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD018"));
    }

    [TestMethod]
    public void MD018_WhenInCodeBlockThenNoViolation()
    {
        var rule = new MD018_NoMissingSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("```\n#NoSpace\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD018_WhenInFrontMatterThenNoViolation()
    {
        var rule = new MD018_NoMissingSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("---\n#tag\n---\n\n# Title");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD018_ViolationMessageDescribesIssue()
    {
        var rule = new MD018_NoMissingSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("#NoSpace");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("No space after hash", violations[0].Message);
    }

    #endregion

    #region MD019 - Multiple Spaces After Hash

    [TestMethod]
    public void MD019_WhenSingleSpaceAfterHashThenNoViolation()
    {
        var rule = new MD019_NoMultipleSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1\n\n## Heading 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD019_WhenMultipleSpacesAfterHashThenReportsViolation()
    {
        var rule = new MD019_NoMultipleSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("#  Heading 1\n\n##  Heading 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD019"));
    }

    [TestMethod]
    public void MD019_WhenSetextHeadingThenNoViolation()
    {
        var rule = new MD019_NoMultipleSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("Heading\n=======");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD019_ViolationMessageDescribesIssue()
    {
        var rule = new MD019_NoMultipleSpaceAtx();
        var analysis = new MarkdownDocumentAnalysis("#  Multiple Spaces");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Multiple spaces", violations[0].Message);
    }

    #endregion

    #region MD020 - No Space In Closed ATX

    [TestMethod]
    public void MD020_WhenSpaceInsideHashesThenNoViolation()
    {
        var rule = new MD020_NoMissingSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1 #\n\n## Heading 2 ##");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD020_WhenNoSpaceBeforeClosingHashThenReportsViolation()
    {
        var rule = new MD020_NoMissingSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1#");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD020", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD020_WhenInCodeBlockThenNoViolation()
    {
        var rule = new MD020_NoMissingSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("```\n# NoSpace#\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD020_ViolationMessageDescribesIssue()
    {
        var rule = new MD020_NoMissingSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading#");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("No space inside hashes", violations[0].Message);
    }

    #endregion

    #region MD021 - Multiple Spaces In Closed ATX

    [TestMethod]
    public void MD021_WhenSingleSpaceInsideHashesThenNoViolation()
    {
        var rule = new MD021_NoMultipleSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1 #\n\n## Heading 2 ##");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD021_WhenMultipleSpacesBeforeClosingHashThenReportsViolation()
    {
        var rule = new MD021_NoMultipleSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1  #");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD021", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD021_WhenMultipleSpacesAfterOpeningHashThenReportsViolation()
    {
        var rule = new MD021_NoMultipleSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("#  Heading 1 #");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD021", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD021_ViolationMessageDescribesIssue()
    {
        var rule = new MD021_NoMultipleSpaceClosedAtx();
        var analysis = new MarkdownDocumentAnalysis("# Heading  #");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Multiple spaces", violations[0].Message);
    }

    #endregion

    #region MD022 - Blanks Around Headings

    [TestMethod]
    public void MD022_WhenBlankLinesAroundHeadingThenNoViolation()
    {
        var rule = new MD022_BlanksAroundHeadings();
        var analysis = new MarkdownDocumentAnalysis("# Heading\n\nText\n\n## Another Heading\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD022_WhenNoBlankLineBeforeHeadingThenReportsViolation()
    {
        var rule = new MD022_BlanksAroundHeadings();
        var analysis = new MarkdownDocumentAnalysis("# First Heading\n\nText\n## Second Heading");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("preceded by", violations[0].Message);
    }

    [TestMethod]
    public void MD022_WhenNoBlankLineAfterHeadingThenReportsViolation()
    {
        var rule = new MD022_BlanksAroundHeadings();
        var analysis = new MarkdownDocumentAnalysis("# Heading\nText immediately after");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("followed by", violations[0].Message);
    }

    [TestMethod]
    public void MD022_WhenFirstHeadingAtDocumentStartThenNoViolationForAbove()
    {
        var rule = new MD022_BlanksAroundHeadings();
        var analysis = new MarkdownDocumentAnalysis("# Heading\n\nText");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // First heading at start doesn't need blank line above
        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD022_WhenLinesAboveSetTo2ThenRequires2BlankLines()
    {
        var rule = new MD022_BlanksAroundHeadings();
        var config = new RuleConfiguration();
        config.Parameters["lines_above"] = "2";
        var analysis = new MarkdownDocumentAnalysis("# First\n\nText\n\n## Second");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // Only 1 blank line above ## Second, but 2 required
        Assert.HasCount(1, violations);
        Assert.Contains("2 blank line", violations[0].Message);
    }

    [TestMethod]
    public void MD022_WhenLinesBelowSetTo0ThenNoBlankLineRequired()
    {
        var rule = new MD022_BlanksAroundHeadings();
        var config = new RuleConfiguration();
        config.Parameters["lines_below"] = "0";
        var analysis = new MarkdownDocumentAnalysis("# Heading\nText immediately after");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD022_WhenSetextHeadingThenChecksCorrectly()
    {
        var rule = new MD022_BlanksAroundHeadings();
        // Setext heading at start of document with blank line after
        var markdown = "Setext Heading\n============\n\nMore text";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Setext heading at document start should not need blank line above
        // And has blank line after, so should be ok
        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD023 - Heading Start Left

    [TestMethod]
    public void MD023_WhenHeadingStartsAtBeginningThenNoViolation()
    {
        var rule = new MD023_HeadingStartLeft();
        var analysis = new MarkdownDocumentAnalysis("# Heading\n\nText");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD023_WhenHeadingIndentedThenReportsViolation()
    {
        var rule = new MD023_HeadingStartLeft();
        var analysis = new MarkdownDocumentAnalysis("  # Indented heading");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD023", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD023_WhenInCodeBlockThenNoViolation()
    {
        var rule = new MD023_HeadingStartLeft();
        var analysis = new MarkdownDocumentAnalysis("```\n  # Comment in code\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD023_ViolationMessageDescribesIssue()
    {
        var rule = new MD023_HeadingStartLeft();
        var analysis = new MarkdownDocumentAnalysis("  # Indented");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("beginning of the line", violations[0].Message);
    }

    #endregion

    #region MD024 - No Duplicate Heading

    [TestMethod]
    public void MD024_WhenUniqueHeadingsThenNoViolation()
    {
        var rule = new MD024_NoDuplicateHeading();
        var analysis = new MarkdownDocumentAnalysis("# Heading 1\n\n## Heading 2\n\n### Heading 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD024_WhenDuplicateHeadingsThenReportsViolation()
    {
        var rule = new MD024_NoDuplicateHeading();
        var analysis = new MarkdownDocumentAnalysis("# Some text\n\n## Some text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD024", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD024_WhenSiblingsOnlyThenAllowsDifferentLevelDuplicates()
    {
        // Per docs: with siblings_only, duplicates at different levels are allowed
        var rule = new MD024_NoDuplicateHeading();
        var config = new RuleConfiguration();
        config.Parameters["siblings_only"] = "true";
        var analysis = new MarkdownDocumentAnalysis("# Features\n\n## Features");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD024_WhenSiblingsOnlyAndSameLevelDuplicatesThenReportsViolation()
    {
        var rule = new MD024_NoDuplicateHeading();
        var config = new RuleConfiguration();
        config.Parameters["siblings_only"] = "true";
        var analysis = new MarkdownDocumentAnalysis("## Features\n\nText\n\n## Features");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD024_ViolationMessageContainsHeadingText()
    {
        var rule = new MD024_NoDuplicateHeading();
        var analysis = new MarkdownDocumentAnalysis("# Duplicate\n\n## Duplicate");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("duplicate", violations[0].Message.ToLower());
    }

    #endregion

    #region MD025 - Single Top-Level Heading

    [TestMethod]
    public void MD025_WhenSingleH1ThenNoViolation()
    {
        var rule = new MD025_SingleTitle();
        var analysis = new MarkdownDocumentAnalysis("# Title\n\n## Section 1\n\n## Section 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD025_WhenMultipleH1ThenReportsViolation()
    {
        var rule = new MD025_SingleTitle();
        var analysis = new MarkdownDocumentAnalysis("# Title 1\n\n# Title 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD025", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD025_WhenLevelSetTo2ThenChecksH2()
    {
        // Per docs: level parameter changes which heading level is checked
        var rule = new MD025_SingleTitle();
        var config = new RuleConfiguration();
        config.Parameters["level"] = "2";
        var analysis = new MarkdownDocumentAnalysis("# Title\n\n## Section 1\n\n## Section 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        // Two H2s when level=2 means second one is a violation
        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD025_ViolationMessageDescribesIssue()
    {
        var rule = new MD025_SingleTitle();
        var analysis = new MarkdownDocumentAnalysis("# First\n\n# Second");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Multiple top-level", violations[0].Message);
    }

    [TestMethod]
    public void MD025_WhenThreeH1ThenReportsTwoViolations()
    {
        var rule = new MD025_SingleTitle();
        var analysis = new MarkdownDocumentAnalysis("# Title 1\n\n# Title 2\n\n# Title 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    #endregion

    #region MD026 - Trailing Punctuation in Heading

    [TestMethod]
    public void MD026_WhenNoTrailingPunctuationThenNoViolation()
    {
        var rule = new MD026_NoTrailingPunctuation();
        var analysis = new MarkdownDocumentAnalysis("# This is a heading\n\n## Another heading");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD026_WhenTrailingPeriodThenReportsViolation()
    {
        var rule = new MD026_NoTrailingPunctuation();
        var analysis = new MarkdownDocumentAnalysis("# This is a heading.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD026", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD026_WhenTrailingExclamationThenReportsViolation()
    {
        var rule = new MD026_NoTrailingPunctuation();
        var analysis = new MarkdownDocumentAnalysis("# Important!");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD026_WhenTrailingQuestionMarkThenNoViolation()
    {
        // Per docs: ? is allowed by default
        var rule = new MD026_NoTrailingPunctuation();
        var analysis = new MarkdownDocumentAnalysis("# What is this?");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD026_WhenCustomPunctuationThenUsesIt()
    {
        var rule = new MD026_NoTrailingPunctuation();
        var config = new RuleConfiguration();
        config.Parameters["punctuation"] = ".,;:";  // No ! so exclamation allowed
        var analysis = new MarkdownDocumentAnalysis("# Important!");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD026_WhenClosedAtxWithPunctuationThenReportsViolation()
    {
        var rule = new MD026_NoTrailingPunctuation();
        var analysis = new MarkdownDocumentAnalysis("# This is a heading. #");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD026_ViolationMessageContainsPunctuation()
    {
        var rule = new MD026_NoTrailingPunctuation();
        var analysis = new MarkdownDocumentAnalysis("# Heading!");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("!", violations[0].Message);
    }

    [TestMethod]
    public void MD026_WhenMultipleHeadingsWithPunctuationThenReportsAll()
    {
        var rule = new MD026_NoTrailingPunctuation();
        var analysis = new MarkdownDocumentAnalysis("# First.\n\n## Second!");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    #endregion
}
