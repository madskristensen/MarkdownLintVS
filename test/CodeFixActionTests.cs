using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

/// <summary>
/// Tests for code fix action infrastructure, style rule violations, and fix metadata.
/// Tests the pure logic that doesn't require Visual Studio editor interfaces.
/// </summary>
[TestClass]
public sealed class CodeFixActionTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region Style Rule Violation Detection Tests

    [TestMethod]
    public void MD004_WhenMixedListMarkers_ThenReportsViolationWithExpectedStyle()
    {
        var rule = new MD004_UlStyle();
        var markdown = "- Item 1\n* Item 2\n+ Item 3\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsNotEmpty(violations, "Should detect inconsistent list markers");
        foreach (LintViolation? violation in violations)
        {
            var marker = ViolationMessageParser.ExtractExpectedMarker(violation.Message);
            Assert.IsNotNull(marker, $"Violation message should contain extractable marker: {violation.Message}");
        }
    }

    [TestMethod]
    public void MD049_WhenMixedEmphasisStyle_ThenReportsViolationWithExpectedStyle()
    {
        var rule = new MD049_EmphasisStyle();
        var markdown = "*italic1*\n\n_italic2_\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsNotEmpty(violations, "Should detect inconsistent emphasis style");
        foreach (LintViolation? violation in violations)
        {
            var style = ViolationMessageParser.ExtractExpectedStyle(violation.Message);
            Assert.IsNotNull(style, $"Violation message should contain extractable style: {violation.Message}");
        }
    }

    [TestMethod]
    public void MD050_WhenMixedStrongStyle_ThenReportsViolationWithExpectedStyle()
    {
        var rule = new MD050_StrongStyle();
        var markdown = "**bold1**\n\n__bold2__\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsNotEmpty(violations, "Should detect inconsistent strong style");
        foreach (LintViolation? violation in violations)
        {
            var style = ViolationMessageParser.ExtractExpectedStyle(violation.Message);
            Assert.IsNotNull(style, $"Violation message should contain extractable style: {violation.Message}");
        }
    }

    [TestMethod]
    public void MD004_WhenConsistentListMarkers_ThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var markdown = "- Item 1\n- Item 2\n- Item 3\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD049_WhenConsistentEmphasis_ThenNoViolations()
    {
        var rule = new MD049_EmphasisStyle();
        var markdown = "*italic1*\n\n*italic2*\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD050_WhenConsistentStrong_ThenNoViolations()
    {
        var rule = new MD050_StrongStyle();
        var markdown = "**bold1**\n\n**bold2**\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region Style Violation Metadata Tests

    [TestMethod]
    public void MD004_ViolationMessage_ContainsDashMarker()
    {
        var rule = new MD004_UlStyle();
        // First marker is dash, second is asterisk -> violation expects dash
        var markdown = "- Item 1\n* Item 2\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual('-', ViolationMessageParser.ExtractExpectedMarker(violations[0].Message));
    }

    [TestMethod]
    public void MD049_ViolationMessage_ContainsAsteriskStyle()
    {
        var rule = new MD049_EmphasisStyle();
        // First emphasis is asterisk, second is underscore -> violation expects asterisk
        var markdown = "*italic1*\n\n_italic2_\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsNotEmpty(violations);
        var style = ViolationMessageParser.ExtractExpectedStyle(violations[0].Message);
        Assert.AreEqual("asterisk", style);
    }

    [TestMethod]
    public void MD050_ViolationMessage_ContainsAsteriskStyle()
    {
        var rule = new MD050_StrongStyle();
        // First strong is asterisk, second is underscore -> violation expects asterisk
        var markdown = "**bold1**\n\n__bold2__\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsNotEmpty(violations);
        var style = ViolationMessageParser.ExtractExpectedStyle(violations[0].Message);
        Assert.AreEqual("asterisk", style);
    }

    [TestMethod]
    public void MD004_WhenExplicitAsteriskConfig_ThenExpectsAsterisk()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "asterisk" };
        var markdown = "- Item 1\n- Item 2\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsNotEmpty(violations, "Should flag dash markers when asterisk is expected");
        Assert.AreEqual('*', ViolationMessageParser.ExtractExpectedMarker(violations[0].Message));
    }

    [TestMethod]
    public void MD049_WhenExplicitUnderscoreConfig_ThenExpectsUnderscore()
    {
        var rule = new MD049_EmphasisStyle();
        var config = new RuleConfiguration { Value = "underscore" };
        var markdown = "*italic*\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning, TestContext.CancellationToken).ToList();

        Assert.IsNotEmpty(violations, "Should flag asterisk emphasis when underscore is expected");
        var style = ViolationMessageParser.ExtractExpectedStyle(violations[0].Message);
        Assert.AreEqual("underscore", style);
    }

    #endregion

    #region Analyzer Integration Tests for Fixable Violations

    [TestMethod]
    public void WhenTrailingSpaces_ThenViolationIsForFixableRule()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\nLine with spaces   \n";

        var violations = analyzer.Analyze(markdown, string.Empty, TestContext.CancellationToken).ToList();

        var md009 = violations.Where(v => v.Rule.Id == "MD009").ToList();
        Assert.IsNotEmpty(md009);
        Assert.AreEqual("MD009", md009[0].Rule.Id);
    }

    [TestMethod]
    public void WhenHardTabs_ThenViolationIsForFixableRule()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n\tindented with tab\n";

        var violations = analyzer.Analyze(markdown, string.Empty, TestContext.CancellationToken).ToList();

        var md010 = violations.Where(v => v.Rule.Id == "MD010").ToList();
        Assert.IsNotEmpty(md010);
        Assert.AreEqual("MD010", md010[0].Rule.Id);
    }

    [TestMethod]
    public void WhenNoMissingSpaceAtx_ThenViolationIsForFixableRule()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "#Title without space\n";

        var violations = analyzer.Analyze(markdown, string.Empty, TestContext.CancellationToken).ToList();

        var md018 = violations.Where(v => v.Rule.Id == "MD018").ToList();
        Assert.IsNotEmpty(md018);
        Assert.AreEqual("MD018", md018[0].Rule.Id);
    }

    [TestMethod]
    public void WhenMultipleBlankLines_ThenViolationIsForFixableRule()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n\n\nParagraph\n";

        var violations = analyzer.Analyze(markdown, string.Empty, TestContext.CancellationToken).ToList();

        var md012 = violations.Where(v => v.Rule.Id == "MD012").ToList();
        Assert.IsNotEmpty(md012);
    }

    public TestContext TestContext { get; set; }

    #endregion
}
