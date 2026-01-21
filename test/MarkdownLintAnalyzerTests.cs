using MarkdownLintVS.Linting;

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
}
