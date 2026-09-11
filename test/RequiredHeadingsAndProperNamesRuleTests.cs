using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class RequiredHeadingsAndProperNamesRuleTests
{
    [TestMethod]
    public void MD043_WhenHeadingStructureMatchesThenNoViolations()
    {
        var rule = new MD043_RequiredHeadings();
        var config = new RuleConfiguration { Value = "# Title; ## Overview; ?" };
        var analysis = new MarkdownDocumentAnalysis("# Title\n\n## Overview\n\n### Details");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD043_WhenHeadingIsMissingThenReportsViolation()
    {
        var rule = new MD043_RequiredHeadings();
        var config = new RuleConfiguration { Value = "# Title; ## Overview" };
        var analysis = new MarkdownDocumentAnalysis("# Title");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Heading structure", violations[0].Message);
    }

    [TestMethod]
    public void MD043_WhenHeadingDiffersThenReportsViolation()
    {
        var rule = new MD043_RequiredHeadings();
        var config = new RuleConfiguration { Value = "# Title; ## Overview" };
        var analysis = new MarkdownDocumentAnalysis("# Title\n\n### Other");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Heading structure", violations[0].Message);
    }

    [TestMethod]
    public void MD044_WhenProperNameCasingIsWrongThenReportsFixableViolation()
    {
        var rule = new MD044_ProperNames();
        var config = new RuleConfiguration { Value = "JavaScript, Visual Studio" };
        var analysis = new MarkdownDocumentAnalysis("Use javascript in visual Studio.");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.AreEqual("JavaScript", violations[0].ReplacementText);
    }

    [TestMethod]
    public void MD043_WhenWildcardAllowsOptionalHeadingsThenNoViolations()
    {
        var rule = new MD043_RequiredHeadings();
        var config = new RuleConfiguration { Value = "# Title; *; ## Footer" };
        var analysis = new MarkdownDocumentAnalysis("# Title\n\n## Optional\n\n### Detail\n\n## Footer");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD043_WhenMatchCaseIsFalseThenHeadingComparisonIgnoresCase()
    {
        var rule = new MD043_RequiredHeadings();
        var config = new RuleConfiguration { Value = "# TITLE" };
        var analysis = new MarkdownDocumentAnalysis("# Title");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD044_WhenProperNameIsCorrectThenNoViolations()
    {
        var rule = new MD044_ProperNames();
        var config = new RuleConfiguration { Value = "JavaScript" };
        var analysis = new MarkdownDocumentAnalysis("JavaScript");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD044_WhenCodeBlocksExcludedThenIgnoresCode()
    {
        var rule = new MD044_ProperNames();
        var config = new RuleConfiguration { Value = "JavaScript" };
        config.Parameters["code_blocks"] = "false";
        var analysis = new MarkdownDocumentAnalysis("`javascript`\n\n```\njavascript\n```");

        List<LintViolation> violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }
}
