using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

/// <summary>
/// Tests demonstrating how IMarkdownLintAnalyzer enables testable architecture.
/// These tests run WITHOUT any Visual Studio dependencies.
/// </summary>
[TestClass]
public sealed class AnalyzerTestabilityTests
{
    /// <summary>
    /// Demonstrates that we can test the analyzer interface contract.
    /// </summary>
    [TestMethod]
    public void Analyzer_ImplementsInterface()
    {
        IMarkdownLintAnalyzer analyzer = MarkdownLintAnalyzer.Instance;

        // Verify the interface is properly implemented
        Assert.IsNotNull(analyzer);
        Assert.IsNotNull(analyzer.Rules);
    }

    /// <summary>
    /// Demonstrates that we can test rules through the interface.
    /// </summary>
    [TestMethod]
    public void Analyzer_RulesCollection_ContainsExpectedRules()
    {
        IMarkdownLintAnalyzer analyzer = MarkdownLintAnalyzer.Instance;

        var ruleIds = analyzer.Rules.Select(r => r.Info.Id).ToList();

        // Verify expected rules are present
        CollectionAssert.Contains(ruleIds, "MD001"); // Heading increment
        CollectionAssert.Contains(ruleIds, "MD009"); // Trailing spaces
        CollectionAssert.Contains(ruleIds, "MD022"); // Blanks around headings
        CollectionAssert.Contains(ruleIds, "MD047"); // Final newline
    }

    /// <summary>
    /// Demonstrates that we can test analysis without VS dependencies.
    /// </summary>
    [TestMethod]
    public void Analyzer_CanAnalyzeMarkdown_WithoutVsDependencies()
    {
        IMarkdownLintAnalyzer analyzer = MarkdownLintAnalyzer.Instance;
        var markdown = "# Heading\nText with trailing spaces   \n";

        var violations = analyzer.Analyze(markdown, null).ToList();

        // Should detect trailing spaces (MD009)
        Assert.IsTrue(violations.Any(v => v.Rule.Id == "MD009"));
    }

    /// <summary>
    /// Demonstrates testing rule behavior in isolation.
    /// </summary>
    [TestMethod]
    public void IndividualRule_CanBeTestedDirectly()
    {
        // Create rule instance directly - no analyzer needed
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line with spaces   \n");
        var config = new RuleConfiguration();

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD009", violations[0].Rule.Id);
    }

    /// <summary>
    /// Demonstrates that MarkdownDocumentAnalysis is fully testable.
    /// </summary>
    [TestMethod]
    public void DocumentAnalysis_ProvidesTestableHelpers()
    {
        var markdown = "---\ntitle: Test\n---\n\n# Heading\n\n```code\ncode block\n```\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        // Test front matter detection
        Assert.IsTrue(analysis.IsLineInFrontMatter(0)); // ---
        Assert.IsTrue(analysis.IsLineInFrontMatter(1)); // title: Test
        Assert.IsTrue(analysis.IsLineInFrontMatter(2)); // ---
        Assert.IsFalse(analysis.IsLineInFrontMatter(4)); // # Heading

        // Test code block detection
        Assert.IsTrue(analysis.IsLineInCodeBlock(6)); // ```code
        Assert.IsTrue(analysis.IsLineInCodeBlock(7)); // code block
        Assert.IsTrue(analysis.IsLineInCodeBlock(8)); // ```
        Assert.IsFalse(analysis.IsLineInCodeBlock(4)); // # Heading
    }

    /// <summary>
    /// Demonstrates GetAnalyzableLines helper is testable.
    /// </summary>
    [TestMethod]
    public void GetAnalyzableLines_SkipsFrontMatterAndCodeBlocks()
    {
        var markdown = "---\ntitle: Test\n---\n\n# Heading\n\n```\ncode\n```\n\nText\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var analyzableLines = analysis.GetAnalyzableLines().ToList();

        // Should skip front matter (lines 0-2) and code block (lines 6-8)
        // Should include: blank line (3), heading (4), blank line (5), blank line (9), text (10)
        Assert.IsFalse(analyzableLines.Any(l => l.Line.Contains("title:")));
        Assert.IsFalse(analyzableLines.Any(l => l.Line == "```"));
        Assert.IsTrue(analyzableLines.Any(l => l.Line == "# Heading"));
        Assert.IsTrue(analyzableLines.Any(l => l.Line == "Text"));
    }
}
