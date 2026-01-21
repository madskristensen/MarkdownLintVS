using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class ListRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    #region MD004 - Unordered List Style

    [TestMethod]
    public void MD004_WhenConsistentDashStyleThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n- item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenConsistentAsteriskStyleThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("* item 1\n* item 2\n* item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenConsistentPlusStyleThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("+ item 1\n+ item 2\n+ item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenMixedStylesThenReportsViolation()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n* item 2\n+ item 3");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.AreEqual("MD004", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD004_WhenConfiguredDashStyleThenReportsOtherMarkers()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "dash" };
        var analysis = new MarkdownDocumentAnalysis("* item 1\n+ item 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD004"));
    }

    [TestMethod]
    public void MD004_WhenConfiguredAsteriskStyleThenReportsOtherMarkers()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "asterisk" };
        var analysis = new MarkdownDocumentAnalysis("- item 1\n+ item 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD004"));
    }

    [TestMethod]
    public void MD004_WhenStyleDisabledThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "false" };
        var analysis = new MarkdownDocumentAnalysis("- item 1\n* item 2\n+ item 3");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_ViolationMessageContainsExpectedMarkerName()
    {
        var rule = new MD004_UlStyle();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n* item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("expected 'dash'", violations[0].Message);
        Assert.Contains("found 'asterisk'", violations[0].Message);
    }

    [TestMethod]
    public void MD004_WhenSublistStyleWithDifferentMarkersPerLevelThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "sublist" };
        // Per docs: outer uses asterisk, middle uses plus, inner uses dash
        var markdown =
            "* Item 1\n" +
            "  + Item 2\n" +
            "    - Item 3\n" +
            "  + Item 4\n" +
            "* Item 5\n" +
            "  + Item 6";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenSublistStyleWithSameMarkersPerLevelThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "sublist" };
        // Sublist allows same marker at same level, just needs consistency
        var markdown =
            "- Item 1\n" +
            "- Item 2\n" +
            "  - Nested 1\n" +
            "  - Nested 2";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD004_WhenSublistStyleWithInconsistentSameLevelThenReportsViolation()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "sublist" };
        // Mixed markers at the same indent level
        var markdown =
            "- Item 1\n" +
            "* Item 2";  // Same level but different marker

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD004_WhenConfiguredPlusStyleThenReportsOtherMarkers()
    {
        var rule = new MD004_UlStyle();
        var config = new RuleConfiguration { Value = "plus" };
        var analysis = new MarkdownDocumentAnalysis("- item 1\n* item 2");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD004"));
    }

    [TestMethod]
    public void MD004_WhenNestedListsWithConsistentStyleThenNoViolations()
    {
        var rule = new MD004_UlStyle();
        var markdown =
            "- Item 1\n" +
            "  - Nested 1\n" +
            "    - Deep nested\n" +
            "  - Nested 2\n" +
            "- Item 2";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region MD005 - List Indent

    [TestMethod]
    public void MD005_WhenConsistentIndentationThenNoViolations()
    {
        var rule = new MD005_ListIndent();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n  - nested 1\n  - nested 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD005_WhenInconsistentIndentationThenReportsViolation()
    {
        var rule = new MD005_ListIndent();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n  - nested 1\n   - nested 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD005", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD005_WhenOrderedListLeftAlignedThenNoViolations()
    {
        var rule = new MD005_ListIndent();
        // Standard left-aligned ordered list
        var markdown =
            "1. Item\n" +
            "2. Item\n" +
            "10. Item\n" +
            "11. Item";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD005_WhenOrderedListRightAlignedThenNoViolations()
    {
        var rule = new MD005_ListIndent();
        // Right-aligned ordered list (same ending column for marker)
        // Per MD005 spec: " 8. Item" and "10. Item" are valid right-aligned
        var markdown =
            " 8. Item\n" +
            " 9. Item\n" +
            "10. Item\n" +
            "11. Item";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Right-aligned ordered lists are now supported
        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD005_WhenOrderedListMixedAlignmentThenReportsViolations()
    {
        var rule = new MD005_ListIndent();
        // Truly misaligned list (neither left nor right aligned)
        var markdown =
            "1. Item\n" +
            "  2. Item\n" +  // Indented but marker doesn't align
            "3. Item";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD005_WhenMultipleNestedLevelsConsistentThenNoViolations()
    {
        var rule = new MD005_ListIndent();
        var markdown =
            "- Level 1 Item 1\n" +
            "  - Level 2 Item 1\n" +
            "    - Level 3 Item 1\n" +
            "    - Level 3 Item 2\n" +
            "  - Level 2 Item 2\n" +
            "- Level 1 Item 2";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD005_WhenMultipleNestedLevelsWithMisalignmentThenReportsViolations()
    {
        var rule = new MD005_ListIndent();
        var markdown =
            "- Level 1 Item 1\n" +
            "  - Level 2 Item 1\n" +
            "   - Misaligned Level 2";  // 3 spaces instead of 2

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("Inconsistent indentation", violations[0].Message);
    }

    [TestMethod]
    public void MD005_WhenMixedOrderedUnorderedListsConsistentThenNoViolations()
    {
        var rule = new MD005_ListIndent();
        var markdown =
            "1. Ordered Item 1\n" +
            "   - Unordered nested\n" +
            "   - Unordered nested 2\n" +
            "2. Ordered Item 2";

        var analysis = new MarkdownDocumentAnalysis(markdown);
        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD005_WhenSingleItemListThenNoViolations()
    {
        var rule = new MD005_ListIndent();
        var analysis = new MarkdownDocumentAnalysis("- Single item");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD005_ViolationMessageContainsExpectedAndFoundIndentation()
    {
        var rule = new MD005_ListIndent();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n - item 2");  // 0 vs 1 space

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("expected 0", violations[0].Message);
        Assert.Contains("found 1", violations[0].Message);
    }

    #endregion

    #region MD029 - Ordered List Prefix

    [TestMethod]
    public void MD029_WhenOrderedPrefixesThenNoViolations()
    {
        var rule = new MD029_OlPrefix();
        var analysis = new MarkdownDocumentAnalysis("1. first\n2. second\n3. third");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD029_WhenAllOnesThenNoViolations()
    {
        var rule = new MD029_OlPrefix();
        var analysis = new MarkdownDocumentAnalysis("1. first\n1. second\n1. third");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD029_WhenConfiguredOneStyleThenReportsOrderedNumbers()
    {
        var rule = new MD029_OlPrefix();
        var config = new RuleConfiguration { Value = "one" };
        var analysis = new MarkdownDocumentAnalysis("1. first\n2. second\n3. third");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD029"));
    }

    [TestMethod]
    public void MD029_WhenConfiguredOrderedStyleThenReportsIncorrectNumbers()
    {
        var rule = new MD029_OlPrefix();
        var config = new RuleConfiguration { Value = "ordered" };
        var analysis = new MarkdownDocumentAnalysis("1. first\n1. second\n1. third");

        var violations = rule.Analyze(analysis, config, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.IsTrue(violations.All(v => v.Rule.Id == "MD029"));
    }

    #endregion

    #region MD030 - List Marker Space

    [TestMethod]
    public void MD030_WhenSingleSpaceAfterMarkerThenNoViolations()
    {
        var rule = new MD030_ListMarkerSpace();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD030_WhenMultipleSpacesAfterMarkerThenReportsViolation()
    {
        var rule = new MD030_ListMarkerSpace();
        var analysis = new MarkdownDocumentAnalysis("-  item 1\n-  item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(2, violations);
        Assert.AreEqual("MD030", violations[0].Rule.Id);
    }

    #endregion

    #region MD032 - Blanks Around Lists

    [TestMethod]
    public void MD032_WhenListSurroundedByBlankLinesThenNoViolations()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n- item 1\n- item 2\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD032_WhenNoBlankLineBeforeListThenReportsViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("Some text\n- item 1\n- item 2\n\nMore text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD032", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD032_WhenNoBlankLineAfterListThenReportsViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        // Use a heading after the list to ensure Markdig doesn't treat it as list continuation
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n- item 1\n- item 2\n# Heading");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD032", violations[0].Rule.Id);
    }

    [TestMethod]
    public void MD032_WhenListAtStartOfDocumentThenNoViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("- item 1\n- item 2\n\nSome text");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD032_WhenListAtEndOfDocumentThenNoViolation()
    {
        var rule = new MD032_BlanksAroundLists();
        var analysis = new MarkdownDocumentAnalysis("Some text\n\n- item 1\n- item 2");

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion
}
