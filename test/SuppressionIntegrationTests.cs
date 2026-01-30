using MarkdownLintVS.Linting;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class SuppressionIntegrationTests
{
    private readonly MarkdownLintAnalyzer _analyzer = new();

    private static void AssertHasViolation(IReadOnlyCollection<LintViolation> violations, string ruleId)
    {
        Assert.IsTrue(violations.Any(v => v.Rule.Id == ruleId), $"Expected a violation for rule '{ruleId}'.");
    }

    private static void AssertDoesNotHaveViolation(IReadOnlyCollection<LintViolation> violations, string ruleId)
    {
        Assert.IsFalse(violations.Any(v => v.Rule.Id == ruleId), $"Did not expect a violation for rule '{ruleId}'.");
    }

    [TestMethod]
    public void WhenHeadingIncrementSuppressedThenNoViolation()
    {
        var markdown = @"# Title

<!-- markdownlint-disable MD001 -->
### Skipped Level
<!-- markdownlint-enable -->";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        AssertDoesNotHaveViolation(violations, "MD001");
    }

    [TestMethod]
    public void WhenHeadingIncrementNotSuppressedThenHasViolation()
    {
        var markdown = @"# Title

### Skipped Level";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        AssertHasViolation(violations, "MD001");
    }

    [TestMethod]
    public void WhenTrailingSpacesSuppressedWithDisableLineThenNoViolation()
    {
        var markdown = "# Title\n\nLine with trailing spaces   <!-- markdownlint-disable-line MD009 -->\n";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        AssertDoesNotHaveViolation(violations, "MD009");
    }

    [TestMethod]
    public void WhenLineLengthSuppressedWithDisableNextLineThenNoViolation()
    {
        // Create a line longer than the default 80 character limit
        var longLine = new string('a', 100);
        var markdown = $@"# Title

<!-- markdownlint-disable-next-line MD013 -->
{longLine}

Normal line.
";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        // MD013 should not be reported for the long line
        var lineLengthViolations = violations.Where(v => v.Rule.Id == "MD013").ToList();
        Assert.IsEmpty(lineLengthViolations);
    }

    [TestMethod]
    public void WhenDisableFileUsedThenRuleSuppressedForEntireDocument()
    {
        var markdown = @"<!-- markdownlint-disable-file MD041 -->

This document doesn't start with a heading and that's OK.

## Section

Content here.
";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        AssertDoesNotHaveViolation(violations, "MD041");
    }

    [TestMethod]
    public void WhenDisableAllUsedThenAllRulesSuppressedInRegion()
    {
        var markdown = @"# Title

<!-- markdownlint-disable -->
###Skipped Level With Missing Space
Line with trailing spaces   
	Line with tabs
<!-- markdownlint-enable -->

## Normal Section
";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        // No violations should be reported for the suppressed section
        // But there shouldn't be violations anyway after enable since content is clean
        var suppressedLineViolations = violations.Where(v => v.LineNumber >= 2 && v.LineNumber <= 5).ToList();
        Assert.IsEmpty(suppressedLineViolations);
    }

    [TestMethod]
    public void WhenCaptureRestoreUsedThenStateIsCorrectlyManaged()
    {
        var markdown = @"# Title

<!-- markdownlint-disable MD010 -->
	Line with tab (suppressed)
<!-- markdownlint-capture -->
<!-- markdownlint-disable MD009 -->
	Line with tab (suppressed) and trailing spaces   
<!-- markdownlint-restore -->
	Line with tab (suppressed but trailing spaces would be reported if present)

<!-- markdownlint-enable -->
	Line with tab (not suppressed)
";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        // MD010 should be suppressed until the enable
        // MD009 should only be suppressed between capture and restore
        var md010Violations = violations.Where(v => v.Rule.Id == "MD010").ToList();
        Assert.HasCount(1, md010Violations, "Should have exactly one MD010 violation (on line after enable)");
        Assert.AreEqual(11, md010Violations[0].LineNumber);

        // MD009 in the capture region should be suppressed
        var md009InCaptureRegion = violations.Where(v => v.Rule.Id == "MD009" && v.LineNumber == 6).ToList();
        Assert.IsEmpty(md009InCaptureRegion, "MD009 should be suppressed in capture region");
    }

    [TestMethod]
    public void WhenMultipleRulesDisabledThenAllAreSuppressed()
    {
        var markdown = @"# Title

<!-- markdownlint-disable MD009 MD010 -->
	Tabs and trailing spaces   
<!-- markdownlint-enable -->
";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        // Neither MD009 nor MD010 should be reported for line 3
        var suppressedViolations = violations.Where(v => v.LineNumber == 3 && (v.Rule.Id == "MD009" || v.Rule.Id == "MD010")).ToList();
        Assert.IsEmpty(suppressedViolations);
    }

    [TestMethod]
    public void WhenViolationBeforeSuppressionThenStillReported()
    {
        var markdown = @"	Line with tab before suppression
<!-- markdownlint-disable MD010 -->
	Line with tab after suppression
<!-- markdownlint-enable -->
";

        var violations = _analyzer.Analyze(markdown, string.Empty).ToList();

        // MD010 should be reported for line 0 but not line 2
        var md010Violations = violations.Where(v => v.Rule.Id == "MD010").ToList();
        Assert.HasCount(1, md010Violations);
        Assert.AreEqual(0, md010Violations[0].LineNumber);
    }

    [TestMethod]
    public void WhenSuppressionDocumentAnalysisUsedDirectlyThenSuppressionMapAvailable()
    {
        var markdown = @"# Title
<!-- markdownlint-disable MD001 -->
### Skipped
<!-- markdownlint-enable -->";

        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsNotNull(analysis.Suppressions);
        Assert.IsTrue(analysis.Suppressions.HasSuppressions);
        Assert.IsTrue(analysis.Suppressions.IsRuleSuppressed(2, "MD001"));
        Assert.IsFalse(analysis.Suppressions.IsRuleSuppressed(0, "MD001"));
    }
}
