using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class RuleConfigurationTests
{
    [TestMethod]
    public void WhenNewRuleConfigurationThenEnabledByDefault()
    {
        var config = new RuleConfiguration();

        Assert.IsTrue(config.Enabled);
    }

    [TestMethod]
    public void WhenNewRuleConfigurationThenDefaultSeverityIsWarning()
    {
        var config = new RuleConfiguration();

        Assert.AreEqual(Linting.DiagnosticSeverity.Warning, config.Severity);
    }

    [TestMethod]
    public void WhenGetIntParameterWithValidValueThenReturnsValue()
    {
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "120";

        var result = config.GetIntParameter("line_length", 80);

        Assert.AreEqual(120, result);
    }

    [TestMethod]
    public void WhenGetIntParameterWithMissingKeyThenReturnsDefault()
    {
        var config = new RuleConfiguration();

        var result = config.GetIntParameter("line_length", 80);

        Assert.AreEqual(80, result);
    }

    [TestMethod]
    public void WhenGetIntParameterWithInvalidValueThenReturnsDefault()
    {
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "not_a_number";

        var result = config.GetIntParameter("line_length", 80);

        Assert.AreEqual(80, result);
    }

    [TestMethod]
    public void WhenGetIntParameterWithConfigValueThenReturnsConfigValue()
    {
        var config = new RuleConfiguration { Value = "100" };

        var result = config.GetIntParameter("any_param", 80);

        Assert.AreEqual(100, result);
    }

    [TestMethod]
    public void WhenGetStringParameterWithValidValueThenReturnsValue()
    {
        var config = new RuleConfiguration();
        config.Parameters["style"] = "atx";

        var result = config.GetStringParameter("style", "consistent");

        Assert.AreEqual("atx", result);
    }

    [TestMethod]
    public void WhenGetStringParameterWithMissingKeyThenReturnsDefault()
    {
        var config = new RuleConfiguration();

        var result = config.GetStringParameter("style", "consistent");

        Assert.AreEqual("consistent", result);
    }

    [TestMethod]
    public void WhenGetStringParameterWithConfigValueThenReturnsConfigValue()
    {
        var config = new RuleConfiguration { Value = "setext" };

        var result = config.GetStringParameter("style", "consistent");

        Assert.AreEqual("setext", result);
    }

    [TestMethod]
    public void WhenGetBoolParameterWithTrueValueThenReturnsTrue()
    {
        var config = new RuleConfiguration();
        config.Parameters["strict"] = "true";

        var result = config.GetBoolParameter("strict", false);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void WhenGetBoolParameterWithFalseValueThenReturnsFalse()
    {
        var config = new RuleConfiguration();
        config.Parameters["strict"] = "false";

        var result = config.GetBoolParameter("strict", true);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void WhenGetBoolParameterWithOneValueThenReturnsTrue()
    {
        var config = new RuleConfiguration();
        config.Parameters["strict"] = "1";

        var result = config.GetBoolParameter("strict", false);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void WhenGetBoolParameterWithMissingKeyThenReturnsDefault()
    {
        var config = new RuleConfiguration();

        var result = config.GetBoolParameter("strict", true);

        Assert.IsTrue(result);
    }

    #region ParseRuleConfiguration Severity Tests

    [TestMethod]
    public void WhenSeverityIsErrorThenConfigHasErrorSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("error");

        Assert.AreEqual(DiagnosticSeverity.Error, config.Severity);
    }

    [TestMethod]
    public void WhenSeverityIsWarningThenConfigHasWarningSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("warning");

        Assert.AreEqual(DiagnosticSeverity.Warning, config.Severity);
    }

    [TestMethod]
    public void WhenSeverityIsSuggestionThenConfigHasSuggestionSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("suggestion");

        Assert.AreEqual(DiagnosticSeverity.Suggestion, config.Severity);
    }

    [TestMethod]
    public void WhenSeverityIsNoneThenConfigIsDisabled()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("none");

        Assert.IsFalse(config.Enabled);
    }

    [TestMethod]
    public void WhenSeverityIsSilentThenConfigHasSilentSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("silent");

        Assert.AreEqual(DiagnosticSeverity.Silent, config.Severity);
    }

    [TestMethod]
    public void WhenValueWithSeveritySuffixThenBothAreParsed()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("atx:error");

        Assert.AreEqual("atx", config.Value);
        Assert.AreEqual(DiagnosticSeverity.Error, config.Severity);
    }

    [TestMethod]
    public void WhenValueWithSuggestionSuffixThenSeverityIsSuggestion()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("120:suggestion");

        Assert.AreEqual("120", config.Value);
        Assert.AreEqual(DiagnosticSeverity.Suggestion, config.Severity);
    }

    [TestMethod]
    public void WhenTrueWithErrorSuffixThenEnabledWithErrorSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("true:error");

        Assert.AreEqual("true", config.Value);
        Assert.AreEqual(DiagnosticSeverity.Error, config.Severity);
    }

    [TestMethod]
    public void WhenFalseThenDisabledRegardlessOfSeverity()
    {
        var analyzer = new MarkdownLintAnalyzer();

        RuleConfiguration config = analyzer.ParseRuleConfiguration("false");

        Assert.IsFalse(config.Enabled);
    }

    #endregion

    #region Default Severity Tests

    [TestMethod]
    public void WhenDefaultConfigurationThenSeverityIsWarning()
    {
        var config = new RuleConfiguration();

        Assert.AreEqual(DiagnosticSeverity.Warning, config.Severity);
    }

    [TestMethod]
    public void WhenAllRuleInfosThenDefaultSeverityIsWarning()
    {
        foreach (RuleInfo rule in RuleRegistry.AllRules)
        {
            Assert.AreEqual(
                DiagnosticSeverity.Warning,
                rule.DefaultSeverity,
                $"Rule {rule.Id} ({rule.Name}) should have Warning as default severity");
        }
    }

    #endregion

    #region EditorConfig Cache Tests

    [TestMethod]
    public void WhenAnalyzingTwiceWithSameFilePath_ThenCacheIsUsed()
    {
        // Verifies the cache doesn't cause incorrect results —
        // two analyses of the same content with the same path should produce identical results
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\nSome text   \n";

        var violations1 = analyzer.Analyze(markdown, "test.md", TestContext.CancellationToken).ToList();
        var violations2 = analyzer.Analyze(markdown, "test.md", TestContext.CancellationToken).ToList();

        Assert.HasCount(violations1.Count, violations2);
        for (var i = 0; i < violations1.Count; i++)
        {
            Assert.AreEqual(violations1[i].Rule.Id, violations2[i].Rule.Id);
            Assert.AreEqual(violations1[i].LineNumber, violations2[i].LineNumber);
        }
    }

    [TestMethod]
    public void WhenClearEditorConfigCache_ThenNoException()
    {
        var analyzer = new MarkdownLintAnalyzer();

        // Should not throw even when cache is empty
        analyzer.ClearEditorConfigCache();

        // Should still analyze correctly after clearing
        var violations = analyzer.Analyze("# Title\n", "test.md", TestContext.CancellationToken).ToList();
        Assert.IsNotNull(violations);
    }

    [TestMethod]
    public void WhenClearEditorConfigCacheAfterAnalysis_ThenNextAnalysisStillWorks()
    {
        var analyzer = new MarkdownLintAnalyzer();
        var markdown = "# Title\n\nSome text   \n";

        // First analysis populates the cache
        var violations1 = analyzer.Analyze(markdown, "test.md", TestContext.CancellationToken).ToList();

        // Clear the cache
        analyzer.ClearEditorConfigCache();

        // Second analysis should re-parse and produce the same results
        var violations2 = analyzer.Analyze(markdown, "test.md", TestContext.CancellationToken).ToList();

        Assert.HasCount(violations1.Count, violations2);
    }

    public TestContext TestContext { get; set; }

    #endregion
}
