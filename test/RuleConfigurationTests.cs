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
}
