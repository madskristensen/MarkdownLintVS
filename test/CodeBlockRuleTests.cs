using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class CodeBlockRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD031 - Blanks Around Fences

    [TestMethod]
    public void MD031_WhenFencedCodeSurroundedByBlankLinesThenNoViolations()
    {
        var rule = new MD031_BlanksAroundFences();
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n```\ncode\n```\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD031_WhenNoBlankLineBeforeFenceThenReportsViolation()
    {
        var rule = new MD031_BlanksAroundFences();
        var analysis = new MarkdownDocumentAnalysis("Some text\n```\ncode\n```\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD031", violations[0].Rule.Id);
        Assert.Contains("before", violations[0].FixDescription!);
    }

    [TestMethod]
    public void MD031_WhenNoBlankLineAfterFenceThenReportsViolation()
    {
        var rule = new MD031_BlanksAroundFences();
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n```\ncode\n```\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD031", violations[0].Rule.Id);
        Assert.Contains("after", violations[0].FixDescription!);
    }

    [TestMethod]
    public void MD031_WhenFenceAtStartOfDocumentThenNoViolation()
    {
        var rule = new MD031_BlanksAroundFences();
        var analysis = new MarkdownDocumentAnalysis("```\ncode\n```\n\nSome text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD031_WhenFenceAtEndOfDocumentThenNoViolation()
    {
        var rule = new MD031_BlanksAroundFences();
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n```\ncode\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD040 - Fenced Code Language

    [TestMethod]
    public void MD040_WhenLanguageSpecifiedThenNoViolations()
    {
        var rule = new MD040_FencedCodeLanguage();
        var analysis = new MarkdownDocumentAnalysis("```csharp\nvar x = 1;\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD040_WhenNoLanguageSpecifiedThenReportsViolation()
    {
        var rule = new MD040_FencedCodeLanguage();
        var analysis = new MarkdownDocumentAnalysis("```\nvar x = 1;\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD040", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD040_WhenTildeFenceWithLanguageThenNoViolations()
    {
        var rule = new MD040_FencedCodeLanguage();
        var analysis = new MarkdownDocumentAnalysis("~~~python\nprint('hello')\n~~~");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD040_WhenTildeFenceWithoutLanguageThenReportsViolation()
    {
        var rule = new MD040_FencedCodeLanguage();
        var analysis = new MarkdownDocumentAnalysis("~~~\nprint('hello')\n~~~");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    #endregion

    #region MD048 - Code Fence Style

    [TestMethod]
    public void MD048_WhenConsistentBacktickStyleThenNoViolations()
    {
        var rule = new MD048_CodeFenceStyle();
        var analysis = new MarkdownDocumentAnalysis("```js\ncode\n```\n\n```python\ncode\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD048_WhenConsistentTildeStyleThenNoViolations()
    {
        var rule = new MD048_CodeFenceStyle();
        var analysis = new MarkdownDocumentAnalysis("~~~js\ncode\n~~~\n\n~~~python\ncode\n~~~");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD048_WhenMixedStylesThenReportsViolation()
    {
        var rule = new MD048_CodeFenceStyle();
        var analysis = new MarkdownDocumentAnalysis("```js\ncode\n```\n\n~~~python\ncode\n~~~");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD048", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD048_WhenConfiguredBacktickStyleThenReportsTilde()
    {
        var rule = new MD048_CodeFenceStyle();
        var config = new RuleConfiguration { Value = "backtick" };
        var analysis = new MarkdownDocumentAnalysis("~~~js\ncode\n~~~");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD048_WhenConfiguredTildeStyleThenReportsBacktick()
    {
        var rule = new MD048_CodeFenceStyle();
        var config = new RuleConfiguration { Value = "tilde" };
        var analysis = new MarkdownDocumentAnalysis("```js\ncode\n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD048_WhenStyleDisabledThenNoViolations()
    {
        var rule = new MD048_CodeFenceStyle();
        var config = new RuleConfiguration { Value = "false" };
        var analysis = new MarkdownDocumentAnalysis("```js\ncode\n```\n\n~~~python\ncode\n~~~");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion
}
