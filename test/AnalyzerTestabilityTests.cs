using MarkdownLintVS.Linting;
namespace MarkdownLintVS.Test;

[TestClass]
public sealed class AnalyzerTestabilityTests
{
    [TestMethod]
    public void Analyzer_RulesCollection_IsSynchronizedWithRuleRegistry()
    {
        var analyzerRuleIds = MarkdownLintAnalyzer.Instance.Rules
            .Select(rule => rule.Info.Id)
            .OrderBy(id => id)
            .ToList();
        var registryRuleIds = RuleRegistry.AllRules
            .Select(rule => rule.Id)
            .OrderBy(id => id)
            .ToList();

        CollectionAssert.AreEqual(registryRuleIds, analyzerRuleIds);
    }
}
