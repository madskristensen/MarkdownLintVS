using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownLintAnalyzerTests
{
    private static void AssertHasViolation(IReadOnlyCollection<LintViolation> violations, string ruleId)
    {
        Assert.IsTrue(violations.Any(v => v.Rule.Id == ruleId), $"Expected a violation for rule '{ruleId}'.");
    }

    private static void AssertDoesNotHaveViolation(IReadOnlyCollection<LintViolation> violations, string ruleId)
    {
        Assert.IsFalse(violations.Any(v => v.Rule.Id == ruleId), $"Did not expect a violation for rule '{ruleId}'.");
    }

    [TestMethod]
    public void WhenEmptyTextThenNoViolations()
    {
        var analyzer = new MarkdownLintAnalyzer();

        var violations = analyzer.Analyze("", string.Empty).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void WhenNullTextThenNoViolations()
    {
        var analyzer = new MarkdownLintAnalyzer();

        var violations = analyzer.Analyze(null!, string.Empty).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void WhenValidMarkdownThenAnalyzesSuccessfully()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\nSome paragraph text.\n\n## Section\n\nMore text.\n";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        // Valid markdown should have no violations
        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void WhenOrderedListContainsUnorderedSublistsThenNoListViolations()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown =
            "# Research method\n\n" +
            "For each feature area:\n\n" +
            "1. Establish current capabilities.\n" +
            "2. Identify friction.\n" +
            "3. Compare competing products.\n" +
            "4. Search relevant tickets.\n" +
            "5. Read public comments.\n" +
            "6. Include useful signals.\n" +
            "7. Look for repeated sentiment.\n" +
            "8. Verify alleged gaps.\n" +
            "9. Check implementation status across:\n" +
            "   - Source repositories\n" +
            "   - Source history and pull requests\n" +
            "   - Azure DevOps work items\n" +
            "   - GitHub issues and releases\n" +
            "10. Search using multiple terms.\n" +
            "11. Assign one of these findings:\n" +
            "   - **Not currently found**\n" +
            "   - **Already shipped**\n" +
            "   - **Status could not be verified**\n" +
            "12. Do not recommend duplicate work. Instead:\n" +
            "   - Exclude addressed work.\n" +
            "   - Reframe residual friction.\n" +
            "   - Identify possible overlap.\n" +
            "13. Treat an open issue as evidence of awareness.\n" +
            "14. Treat old work carefully.\n" +
            "15. Summarize private evidence carefully.\n" +
            "16. Distinguish among:\n" +
            "   - verified current gap\n" +
            "   - shipped feature with residual friction\n" +
            "   - implementation status not verified\n" +
            "17. Record important evidence limitations.\n";

        var violations = analyzer.Analyze(markdown, string.Empty)
            .Where(v => v.Rule.Id is "MD004" or "MD005" or "MD007" or "MD029" or "MD030" or "MD032")
            .ToList();

        Assert.IsEmpty(
            violations,
            string.Join(Environment.NewLine, violations.Select(v => $"{v.Rule.Id} line {v.LineNumber + 1}: {v.Message}")));
    }

    [TestMethod]
    public void WhenHeadingSkipsLevelThenReportsViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n### Skipped Level";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        AssertHasViolation(violations, "MD001");
    }

    [TestMethod]
    public void WhenHeadingDoesNotSkipLevelThenNoHeadingIncrementViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n## Section";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        AssertDoesNotHaveViolation(violations, "MD001");
    }

    [TestMethod]
    public void WhenTrailingSpacesThenReportsViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "Line with trailing spaces   ";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        AssertHasViolation(violations, "MD009");
    }

    [TestMethod]
    public void WhenNoTrailingSpacesThenNoTrailingSpacesViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "Line without trailing spaces";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        AssertDoesNotHaveViolation(violations, "MD009");
    }

    [TestMethod]
    public void WhenHardTabsThenReportsViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "Line\twith tab";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        AssertHasViolation(violations, "MD010");
    }

    [TestMethod]
    public void WhenNoHardTabsThenNoHardTabsViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "Spaces only\n    Indented";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        AssertDoesNotHaveViolation(violations, "MD010");
    }

    [TestMethod]
    public void WhenMultipleViolationsThenReportsAll()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n### Skip\n\nText with trailing   ";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        Assert.IsGreaterThanOrEqualTo(2, violations.Count, "Expected at least two violations.");
        AssertHasViolation(violations, "MD001");
        AssertHasViolation(violations, "MD009");
    }

    [TestMethod]
    public void WhenViolationThenContainsRuleInfo()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n### Skipped";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();
        LintViolation violation = violations.First(v => v.Rule.Id == "MD001");

        Assert.IsNotNull(violation.Rule);
        Assert.AreEqual("MD001", violation.Rule.Id);
        Assert.IsFalse(string.IsNullOrEmpty(violation.Rule.Name));
        Assert.IsFalse(string.IsNullOrEmpty(violation.Rule.Description));
        Assert.IsFalse(string.IsNullOrEmpty(violation.Rule.DocumentationUrl));
    }

    [TestMethod]
    public void WhenViolationThenContainsLocationInfo()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n### Skipped";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();
        LintViolation violation = violations.First(v => v.Rule.Id == "MD001");

        Assert.IsGreaterThanOrEqualTo(0, violation.LineNumber);
        Assert.IsGreaterThanOrEqualTo(0, violation.ColumnStart);
        Assert.IsGreaterThanOrEqualTo(violation.ColumnStart, violation.ColumnEnd);
    }

    [TestMethod]
    public void WhenViolationThenErrorCodeMatchesRuleId()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n### Skipped";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();
        LintViolation violation = violations.First(v => v.Rule.Id == "MD001");

        Assert.AreEqual(violation.Rule.Id, violation.GetErrorCode());
    }

    [TestMethod]
    public void WhenDisabledByDefaultRuleEnabledInOptionsThenConfigurationIsEnabled()
    {
        RuleInfo rule = RuleRegistry.GetRule("MD013");

        RuleConfiguration configuration = MarkdownLintAnalyzer.CreateOptionsConfiguration(rule, enabledFromOptions: true);

        Assert.IsTrue(configuration.Enabled);
    }

    [TestMethod]
    public void WhenRuleDisabledInOptionsThenConfigurationIsDisabled()
    {
        RuleInfo rule = RuleRegistry.GetRule("MD001");

        RuleConfiguration configuration = MarkdownLintAnalyzer.CreateOptionsConfiguration(rule, enabledFromOptions: false);

        Assert.IsFalse(configuration.Enabled);
    }

    [TestMethod]
    public void WhenDocumentIsSmallThenRulesRunSequentially()
    {
        bool result = MarkdownLintAnalyzer.ShouldRunRulesInParallel(textLength: 100, enabledRuleCount: 50);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void WhenFewRulesAreEnabledThenRulesRunSequentially()
    {
        bool result = MarkdownLintAnalyzer.ShouldRunRulesInParallel(textLength: 10_000, enabledRuleCount: 2);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void WhenDocumentAndRuleSetAreLargeThenRulesRunInParallel()
    {
        bool result = MarkdownLintAnalyzer.ShouldRunRulesInParallel(textLength: 10_000, enabledRuleCount: 50);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void WhenParallelRulesAreDisabledThenRulesRunSequentially()
    {
        bool result = MarkdownLintAnalyzer.ShouldRunRulesInParallel(
            textLength: 10_000,
            enabledRuleCount: 50,
            parallelRules: false);

        Assert.IsFalse(result);
    }

    #region ParseRuleConfiguration Tests

    [TestMethod]
    public void ParseRuleConfiguration_WhenWindowsPathThenPreservesFullPath()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration(@"C:\Users\site\docs");

        Assert.AreEqual(@"C:\Users\site\docs", config.Value);
        Assert.IsTrue(config.Enabled);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenPathWithMultipleColonsThenPreservesFullPath()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration(@"D:\Projects\My:Special:Folder");

        Assert.AreEqual(@"D:\Projects\My:Special:Folder", config.Value);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenValueWithSeveritySuffixThenParsesBoth()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("asterisk:error");

        Assert.AreEqual("asterisk", config.Value);
        Assert.AreEqual(DiagnosticSeverity.Error, config.Severity);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenWindowsPathWithSeveritySuffixThenParsesBoth()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration(@"C:\Users\site:warning");

        Assert.AreEqual(@"C:\Users\site", config.Value);
        Assert.AreEqual(DiagnosticSeverity.Warning, config.Severity);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenBooleanFalseThenDisabled()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("false");

        Assert.IsFalse(config.Enabled);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenBooleanTrueThenEnabled()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("true");

        Assert.IsTrue(config.Enabled);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenSeverityOnlyThenSetsSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("error");

        Assert.AreEqual(DiagnosticSeverity.Error, config.Severity);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenSuggestionSeverityThenSetsSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("suggestion");

        Assert.AreEqual(DiagnosticSeverity.Suggestion, config.Severity);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenEmptyThenReturnsDefault()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("");

        Assert.IsTrue(config.Enabled);
        Assert.AreEqual(DiagnosticSeverity.Warning, config.Severity);
        Assert.IsNull(config.Value);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenNullThenReturnsDefault()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration(null!);

        Assert.IsTrue(config.Enabled);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenUnrecognizedValueThenTreatsAsValue()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("consistent");

        Assert.AreEqual("consistent", config.Value);
        Assert.IsTrue(config.Enabled);
    }

    [TestMethod]
    public void ParseRuleConfiguration_WhenColonButNotSeverityThenPreservesValue()
    {
        var analyzer = new MarkdownLintAnalyzer();

        // "test:value" - "value" is not a valid severity, so preserve the whole thing
        RuleConfiguration config = analyzer.ParseRuleConfiguration("test:value");

        Assert.AreEqual("test:value", config.Value);
    }

    #endregion
}
