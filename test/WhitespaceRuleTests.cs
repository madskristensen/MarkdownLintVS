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

    [TestMethod]
    public void MD011_WhenMarkdownExtraFootnoteThenNoViolation()
    {
        // Per docs: Markdown Extra-style footnotes should not trigger
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("For (example)[^1]");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenInFrontMatterThenSkipped()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("---\nlink: (http://example.com)[text]\n---\n\nContent");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD011_WhenMultipleReversedLinksThenReportsAll()
    {
        var rule = new MD011_NoReversedLinks();
        var analysis = new MarkdownDocumentAnalysis("(http://a.com)[A] and (http://b.com)[B]");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    #endregion

    #region MD012 - Multiple Blank Lines

    [TestMethod]
    public void MD012_WhenSingleBlankLineThenNoViolation()
    {
        var rule = new MD012_NoMultipleBlanks();
        var analysis = new MarkdownDocumentAnalysis("Line 1\n\nLine 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD012_WhenMultipleBlankLinesThenReportsViolation()
    {
        var rule = new MD012_NoMultipleBlanks();
        var analysis = new MarkdownDocumentAnalysis("Line 1\n\n\nLine 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD012", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD012_WhenMaximumSetTo2ThenAllows2BlankLines()
    {
        var rule = new MD012_NoMultipleBlanks();
        var config = new RuleConfiguration();
        config.Parameters["maximum"] = "2";
        var analysis = new MarkdownDocumentAnalysis("Line 1\n\n\nLine 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD012_WhenMaximumExceededThenReportsViolation()
    {
        var rule = new MD012_NoMultipleBlanks();
        var config = new RuleConfiguration();
        config.Parameters["maximum"] = "2";
        var analysis = new MarkdownDocumentAnalysis("Line 1\n\n\n\nLine 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD012_WhenInCodeBlockThenNoViolation()
    {
        // Per docs: rule will not be triggered inside code blocks
        var rule = new MD012_NoMultipleBlanks();
        var analysis = new MarkdownDocumentAnalysis("```\n\n\n\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD012_WhenInFrontMatterThenNoViolation()
    {
        var rule = new MD012_NoMultipleBlanks();
        var analysis = new MarkdownDocumentAnalysis("---\n\n\ntitle: Test\n---\n\nContent");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD012_ViolationMessageContainsCounts()
    {
        var rule = new MD012_NoMultipleBlanks();
        var analysis = new MarkdownDocumentAnalysis("Line 1\n\n\n\nLine 2");  // 3 blank lines

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);  // Reports on 2nd and 3rd blank line
        Assert.Contains("maximum 1", violations[0].Message);
    }

    [TestMethod]
    public void MD012_WhenMultipleGroupsOfBlankLinesThenReportsAll()
    {
        var rule = new MD012_NoMultipleBlanks();
        var analysis = new MarkdownDocumentAnalysis("A\n\n\nB\n\n\nC");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
    }

    #endregion

    #region MD013 - Line Length

    [TestMethod]
    public void MD013_WhenLinesWithinLimitThenNoViolation()
    {
        var rule = new MD013_LineLength();
        var analysis = new MarkdownDocumentAnalysis("Short line");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenLineExceeds80CharsThenReportsViolation()
    {
        var rule = new MD013_LineLength();
        // Line with whitespace beyond limit (words separated by spaces)
        var longLine = "This is a very long line with many words that definitely exceeds the eighty character limit for lines";
        var analysis = new MarkdownDocumentAnalysis(longLine);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD013", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD013_WhenCustomLineLengthSetThenUsesIt()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "100";
        var line90 = new string('a', 90);
        var analysis = new MarkdownDocumentAnalysis(line90);

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenHeadingLineLengthSetThenUsesItForHeadings()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "80";
        config.Parameters["heading_line_length"] = "100";
        var longHeading = "# " + new string('a', 90);  // 92 chars total
        var analysis = new MarkdownDocumentAnalysis(longHeading);

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenCodeBlockLineLengthSetThenUsesItForCode()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "80";
        config.Parameters["code_block_line_length"] = "120";
        var longCodeLine = new string('a', 100);
        var analysis = new MarkdownDocumentAnalysis($"```\n{longCodeLine}\n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenCodeBlocksDisabledThenSkipsCodeBlocks()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["code_blocks"] = "false";
        var longCodeLine = new string('a', 100);
        var analysis = new MarkdownDocumentAnalysis($"```\n{longCodeLine}\n```");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenHeadingsDisabledThenSkipsHeadings()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["headings"] = "false";
        var longHeading = "# " + new string('a', 100);
        var analysis = new MarkdownDocumentAnalysis(longHeading);

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenTablesDisabledThenSkipsTables()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["tables"] = "false";
        var longTableRow = "| " + new string('a', 100) + " |";
        var analysis = new MarkdownDocumentAnalysis(longTableRow);

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenStrictModeThenCountsUrlLength()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "50";
        config.Parameters["strict"] = "true";
        // Line with URL that makes it long
        var analysis = new MarkdownDocumentAnalysis("Check out https://example.com/very/long/path/here text");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD013_WhenNotStrictModeAndOnlyUrlLongThenNoViolation()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "30";
        // Line without whitespace beyond limit - should be allowed in non-strict mode
        var analysis = new MarkdownDocumentAnalysis("See-this-very-long-url-without-spaces");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenNoWhitespaceBeyondLimitThenNoViolation()
    {
        // Per docs: exception when no whitespace beyond limit (like long URLs)
        var rule = new MD013_LineLength();
        var longContinuousLine = new string('a', 100);  // No spaces
        var analysis = new MarkdownDocumentAnalysis(longContinuousLine);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenSternModeAndHasWhitespaceBeyondLimitThenReportsViolation()
    {
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "30";
        config.Parameters["stern"] = "true";
        // Line with whitespace beyond limit
        var analysis = new MarkdownDocumentAnalysis("This is a line with spaces that exceeds the limit");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD013_WhenSternModeAndNoWhitespaceBeyondLimitThenNoViolation()
    {
        // Per docs: stern mode allows long lines without spaces
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "30";
        config.Parameters["stern"] = "true";
        var analysis = new MarkdownDocumentAnalysis("This-line-has-no-spaces-beyond-thirty");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenLinkRefDefinitionThenAlwaysExempt()
    {
        // Per docs: link/image reference definitions are always exempted
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "30";
        config.Parameters["strict"] = "true";
        var analysis = new MarkdownDocumentAnalysis("[very-long-reference-label]: https://example.com/very/long/path \"title\"");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenStandaloneLinkThenAlwaysExempt()
    {
        // Per docs: standalone lines with only a link (possibly with emphasis) are exempted
        var rule = new MD013_LineLength();
        var config = new RuleConfiguration();
        config.Parameters["line_length"] = "30";
        config.Parameters["strict"] = "true";
        var analysis = new MarkdownDocumentAnalysis("**[Link text](https://example.com/very/long/path/here)**");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_WhenInFrontMatterThenSkipped()
    {
        var rule = new MD013_LineLength();
        var longLine = "description: " + new string('a', 100);
        var analysis = new MarkdownDocumentAnalysis($"---\n{longLine}\n---\n\nContent");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD013_ViolationMessageContainsLengths()
    {
        var rule = new MD013_LineLength();
        // Line with spaces beyond the 80 char limit
        var analysis = new MarkdownDocumentAnalysis("This is a very long line with many words that definitely exceeds the eighty character limit for lines in documents");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("80", violations[0].Message);
    }

    #endregion

    #region MD014 - Dollar Signs in Commands

    [TestMethod]
    public void MD014_WhenCommandsWithOutputThenNoViolation()
    {
        // Per docs: showing output does not trigger this rule
        var rule = new MD014_CommandsShowOutput();
        var markdown = "```\n$ ls\nfoo bar\n$ cat foo\nHello world\n```";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD014_WhenAllCommandsHaveDollarSignsWithoutOutputThenReportsViolation()
    {
        // Per docs: all commands with $ but no output triggers rule
        var rule = new MD014_CommandsShowOutput();
        var markdown = "```\n$ ls\n$ cat foo\n$ less bar\n```";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(3, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD014"));
    }

    [TestMethod]
    public void MD014_WhenSomeCommandsHaveOutputThenNoViolation()
    {
        // Per docs: if some commands have output, it's not a violation
        var rule = new MD014_CommandsShowOutput();
        var markdown = "```\n$ mkdir test\nmkdir: created directory 'test'\n$ ls test\n```";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD014_WhenNoDollarSignsThenNoViolation()
    {
        var rule = new MD014_CommandsShowOutput();
        var markdown = "```\nls\ncat foo\nless bar\n```";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD014_WhenEmptyCodeBlockThenNoViolation()
    {
        var rule = new MD014_CommandsShowOutput();
        var analysis = new MarkdownDocumentAnalysis("```\n```");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD014_WhenMultipleCodeBlocksThenChecksEach()
    {
        var rule = new MD014_CommandsShowOutput();
        var markdown = "```\n$ cmd1\n```\n\n```\n$ cmd2\noutput\n```";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Only first code block should have violations
        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD014_ViolationMessageDescribesIssue()
    {
        var rule = new MD014_CommandsShowOutput();
        var markdown = "```\n$ ls\n```";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Dollar signs", violations[0].Message);
    }

    #endregion
}
