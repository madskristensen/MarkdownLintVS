using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class WhitespaceRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD009 - Trailing Spaces

    [TestMethod]
    public void MD009_WhenNoTrailingSpacesThenNoViolations()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line without trailing spaces\nAnother line");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_WhenTrailingSpacesThenReportsViolation()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line with trailing spaces   ");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD009", violations[0].Rule.Id);
        Assert.AreEqual(0, violations[0].LineNumber);
    }

    [TestMethod]
    public void MD009_WhenTwoSpacesForLineBreakThenNoViolationByDefault()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line with two trailing spaces  ");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_WhenStrictModeThenReportsTwoSpaces()
    {
        var rule = new MD009_NoTrailingSpaces();
        var config = new RuleConfiguration();
        config.Parameters["strict"] = "true";
        var analysis = new MarkdownDocumentAnalysis("Line with two trailing spaces  ");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD009_WhenInCodeBlockThenSkipped()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("```\ncode with spaces   \n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_WhenCodeBlocksEnabledThenReportsCodeBlockSpaces()
    {
        var rule = new MD009_NoTrailingSpaces();
        var config = new RuleConfiguration();
        config.Parameters["code_blocks"] = "true";
        var analysis = new MarkdownDocumentAnalysis("```\ncode with spaces   \n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD009_WhenBrSpacesSetTo3ThenAllows3Spaces()
    {
        var rule = new MD009_NoTrailingSpaces();
        var config = new RuleConfiguration();
        config.Parameters["br_spaces"] = "3";
        var analysis = new MarkdownDocumentAnalysis("Line with three spaces   ");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_WhenBrSpacesSetTo1ThenNoExceptionAllowed()
    {
        // Per docs: br_spaces must be >= 2 to take effect, value of 1 behaves like 0
        var rule = new MD009_NoTrailingSpaces();
        var config = new RuleConfiguration();
        config.Parameters["br_spaces"] = "1";
        var analysis = new MarkdownDocumentAnalysis("Line with one space ");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD009_WhenListItemEmptyLinesThenAllowsSpacesInListBlankLines()
    {
        // Per docs: allows spaces for empty lines in list items when enabled
        var rule = new MD009_NoTrailingSpaces();
        var config = new RuleConfiguration();
        config.Parameters["list_item_empty_lines"] = "true";
        var analysis = new MarkdownDocumentAnalysis("- list item text\n  \n  list item text");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_WhenMultipleLinesThenReportsAllViolations()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line 1   \nLine 2\nLine 3   ");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.AreEqual(0, violations[0].LineNumber);
        Assert.AreEqual(2, violations[1].LineNumber);
    }

    [TestMethod]
    public void MD009_WhenInFrontMatterThenSkipped()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("---\ntitle: Test   \n---\n\nContent");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD009_ViolationMessageContainsSpaceCount()
    {
        var rule = new MD009_NoTrailingSpaces();
        var analysis = new MarkdownDocumentAnalysis("Line with spaces     ");  // 5 spaces

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("5", violations[0].Message);
    }

    #endregion

    #region MD010 - Hard Tabs

    [TestMethod]
    public void MD010_WhenNoTabsThenNoViolations()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("No tabs here\n    Spaces only");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD010_WhenTabPresentThenReportsViolation()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("Line\twith tab");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD010", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD010_WhenMultipleTabsThenReportsEachViolation()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("Line\twith\ttabs");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    [TestMethod]
    public void MD010_WhenCodeBlocksDisabledThenSkipsCodeBlocks()
    {
        var rule = new MD010_NoHardTabs();
        var config = new RuleConfiguration();
        config.Parameters["code_blocks"] = "false";
        var analysis = new MarkdownDocumentAnalysis("```\ncode\twith\ttab\n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD010_WhenCodeBlocksEnabledThenReportsTabsInCodeBlocks()
    {
        // Per docs: code_blocks defaults to true
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("```\ncode\twith tab\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD010_WhenIgnoreCodeLanguageThenSkipsThoseBlocks()
    {
        // Per docs: ignore_code_languages can specify languages to skip
        var rule = new MD010_NoHardTabs();
        var config = new RuleConfiguration();
        config.Parameters["ignore_code_languages"] = "makefile,go";
        var analysis = new MarkdownDocumentAnalysis("```makefile\nall:\n\techo hello\n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD010_WhenIgnoreCodeLanguageDoesntMatchThenReportsViolation()
    {
        var rule = new MD010_NoHardTabs();
        var config = new RuleConfiguration();
        config.Parameters["ignore_code_languages"] = "makefile,go";
        var analysis = new MarkdownDocumentAnalysis("```python\ncode\twith tab\n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD010_WhenSpacesPerTabSetThenViolationMessageReflectsIt()
    {
        var rule = new MD010_NoHardTabs();
        var config = new RuleConfiguration();
        config.Parameters["spaces_per_tab"] = "2";
        var analysis = new MarkdownDocumentAnalysis("Line\twith tab");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("2 spaces", violations[0].FixDescription);
    }

    [TestMethod]
    public void MD010_WhenInFrontMatterThenSkipped()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("---\ntitle:\tTest\n---\n\nContent");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD010_WhenTabAtStartOfLineThenReportsViolation()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("\tIndented with tab");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual(0, violations[0].ColumnStart);
    }

    [TestMethod]
    public void MD010_WhenMultipleLinesWithTabsThenReportsAll()
    {
        var rule = new MD010_NoHardTabs();
        var analysis = new MarkdownDocumentAnalysis("Line 1\twith tab\nLine 2\nLine 3\twith tab");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.AreEqual(0, violations[0].LineNumber);
        Assert.AreEqual(2, violations[1].LineNumber);
    }

    [TestMethod]
    public void MD010_WhenIndentedCodeBlockWithTabsThenReportsByDefault()
    {
        var rule = new MD010_NoHardTabs();
        // 4-space indented code block with tab inside
        var analysis = new MarkdownDocumentAnalysis("    code\twith tab");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    #endregion

    #region MD011 - Reversed Links

    [TestMethod]
    public void MD011_WhenCorrectLinkSyntaxThenNoViolations()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("[Link text](http://example.com)");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenReversedLinkSyntaxThenReportsViolation()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("(http://example.com)[Link text]");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD011", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD011_WhenInCodeBlockThenSkipped()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("```\n(http://example.com)[Link text]\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenParenthesesFollowedByArrayIndexThenNoViolation()
    {
        // Valid: function call or value in parens followed by array-like indexing
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("Use (value)[0] to get the first element");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenParenthesesFollowedByPropertyAccessThenNoViolation()
    {
        // Valid: expression in parens followed by property bracket notation
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("The (obj)[key] syntax accesses properties");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenCitationWithFootnoteThenNoViolation()
    {
        // Valid: citation reference that happens to have parens before brackets
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("According to (Smith, 2020)[1], this is true.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenReversedRelativePathLinkThenReportsViolation()
    {
        // Reversed relative path links should be flagged
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("See (./docs/readme.md)[the docs] for more info.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD011", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD011_WhenReversedParentPathLinkThenReportsViolation()
    {
        // Reversed parent-relative path links should be flagged
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("See (../other/file.md)[other file] for details.");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD011", violations[0].Rule.Id);
    }

    #endregion
}
