using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownlintCompatibilityTests
{
    [TestMethod]
    [DataRow("MD001", "# Heading\n### Skipped level", 1, DisplayName = "MD001 rejects skipped heading levels")]
    [DataRow("MD009", "Text with trailing spaces   ", 1, DisplayName = "MD009 rejects trailing spaces")]
    [DataRow("MD009", "Markdown hard break  ", 0, DisplayName = "MD009 permits a two-space hard break")]
    [DataRow("MD010", "Text\twith tab", 1, DisplayName = "MD010 rejects hard tabs")]
    [DataRow("MD012", "First\n\n\nSecond", 1, DisplayName = "MD012 reports one consecutive blank run")]
    [DataRow("MD018", "#Missing space", 1, DisplayName = "MD018 requires a space after the hash")]
    [DataRow("MD019", "#  Too many spaces", 1, DisplayName = "MD019 rejects multiple spaces after the hash")]
    [DataRow("MD023", "  # Indented heading", 1, DisplayName = "MD023 rejects indented headings")]
    [DataRow("MD025", "# First\n\n# Second", 1, DisplayName = "MD025 permits one top-level heading")]
    [DataRow("MD034", "Visit https://example.com", 1, DisplayName = "MD034 rejects bare URLs")]
    [DataRow("MD042", "[link]()", 1, DisplayName = "MD042 rejects empty link destinations")]
    [DataRow("MD047", "No final newline", 1, DisplayName = "MD047 requires a final newline")]
    public void CanonicalScenarioMatchesMarkdownlint(
        string ruleId,
        string markdown,
        int expectedViolationCount)
    {
        IMarkdownRule rule = MarkdownLintAnalyzer.Instance.Rules.Single(
            candidate => candidate.Info.Id == ruleId);
        var analysis = new MarkdownDocumentAnalysis(markdown);

        List<LintViolation> violations = rule.Analyze(
            analysis,
            new RuleConfiguration(),
            DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(expectedViolationCount, violations);
    }
}
