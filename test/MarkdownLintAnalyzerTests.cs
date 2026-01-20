using MarkdownLintVS.Linting;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownLintAnalyzerTests
{
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

        Assert.IsTrue(violations.Any(v => v.Rule.Id == "MD001"));
    }

    [TestMethod]
    public void WhenTrailingSpacesThenReportsViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "Line with trailing spaces   ";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        Assert.IsTrue(violations.Any(v => v.Rule.Id == "MD009"));
    }

    [TestMethod]
    public void WhenHardTabsThenReportsViolation()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "Line\twith tab";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        Assert.IsTrue(violations.Any(v => v.Rule.Id == "MD010"));
    }

    [TestMethod]
    public void WhenMultipleViolationsThenReportsAll()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\n### Skip\n\nText with trailing   ";

        var violations = analyzer.Analyze(markdown, string.Empty).ToList();

        Assert.IsGreaterThanOrEqualTo(2, violations.Count);
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
