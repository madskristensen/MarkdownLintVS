using MarkdownLintVS.Linting;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class SuppressionCommentParserTests
{
    private readonly SuppressionCommentParser _parser = new();

    #region disable/enable tests

    [TestMethod]
    public void WhenDisableAllThenAllRulesSuppressed()
    {
        var lines = new[]
        {
            "# Title",
            "<!-- markdownlint-disable -->",
            "Content with issues",
            "More content",
            "<!-- markdownlint-enable -->",
            "Back to normal"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.AreAllRulesSuppressed(0), "Line 0 should not be suppressed");
        Assert.IsTrue(map.AreAllRulesSuppressed(1), "Line 1 (disable comment) should be suppressed");
        Assert.IsTrue(map.AreAllRulesSuppressed(2), "Line 2 should be suppressed");
        Assert.IsTrue(map.AreAllRulesSuppressed(3), "Line 3 should be suppressed");
        Assert.IsFalse(map.AreAllRulesSuppressed(4), "Line 4 (enable comment) should not be suppressed");
        Assert.IsFalse(map.AreAllRulesSuppressed(5), "Line 5 should not be suppressed");
    }

    [TestMethod]
    public void WhenDisableSpecificRulesThenOnlyThoseRulesSuppressed()
    {
        var lines = new[]
        {
            "# Title",
            "<!-- markdownlint-disable MD001 MD009 -->",
            "Content here",
            "<!-- markdownlint-enable -->",
            "Normal content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.IsRuleSuppressed(0, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"), "MD001 should be suppressed on line 1");
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD009"), "MD009 should be suppressed on line 1");
        Assert.IsTrue(map.IsRuleSuppressed(2, "MD001"), "MD001 should be suppressed on line 2");
        Assert.IsTrue(map.IsRuleSuppressed(2, "MD009"), "MD009 should be suppressed on line 2");
        Assert.IsFalse(map.IsRuleSuppressed(2, "MD013"), "MD013 should not be suppressed on line 2");
        Assert.IsFalse(map.IsRuleSuppressed(3, "MD001"), "MD001 should not be suppressed after enable");
    }

    [TestMethod]
    public void WhenEnableSpecificRuleThenOnlyThatRuleReEnabled()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable MD001 MD009 -->",
            "Both suppressed",
            "<!-- markdownlint-enable MD001 -->",
            "Only MD009 suppressed",
            "<!-- markdownlint-enable -->",
            "None suppressed"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD009"));
        Assert.IsFalse(map.IsRuleSuppressed(3, "MD001"), "MD001 should be re-enabled");
        Assert.IsTrue(map.IsRuleSuppressed(3, "MD009"), "MD009 should still be suppressed");
        Assert.IsFalse(map.IsRuleSuppressed(5, "MD009"), "MD009 should be re-enabled after enable-all");
    }

    #endregion

    #region disable-line tests

    [TestMethod]
    public void WhenDisableLineThenOnlyThatLineSuppressed()
    {
        var lines = new[]
        {
            "Normal line",
            "Suppressed line <!-- markdownlint-disable-line -->",
            "Normal again"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.AreAllRulesSuppressed(0));
        Assert.IsTrue(map.AreAllRulesSuppressed(1));
        Assert.IsFalse(map.AreAllRulesSuppressed(2));
    }

    [TestMethod]
    public void WhenDisableLineWithRulesThenOnlyThoseRulesSuppressedOnLine()
    {
        var lines = new[]
        {
            "Normal line",
            "Long line <!-- markdownlint-disable-line MD013 -->",
            "Normal again"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.IsRuleSuppressed(0, "MD013"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD013"), "MD013 should be suppressed on line 1");
        Assert.IsFalse(map.IsRuleSuppressed(1, "MD001"), "MD001 should not be suppressed on line 1");
        Assert.IsFalse(map.IsRuleSuppressed(2, "MD013"));
    }

    #endregion

    #region disable-next-line tests

    [TestMethod]
    public void WhenDisableNextLineThenNextLineSuppressed()
    {
        var lines = new[]
        {
            "Normal line",
            "<!-- markdownlint-disable-next-line -->",
            "This line is suppressed",
            "Normal again"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.AreAllRulesSuppressed(0));
        Assert.IsFalse(map.AreAllRulesSuppressed(1));
        Assert.IsTrue(map.AreAllRulesSuppressed(2));
        Assert.IsFalse(map.AreAllRulesSuppressed(3));
    }

    [TestMethod]
    public void WhenDisableNextLineWithRulesThenOnlyThoseRulesSuppressedOnNextLine()
    {
        var lines = new[]
        {
            "Normal line",
            "<!-- markdownlint-disable-next-line MD013 MD033 -->",
            "This line has MD013 and MD033 suppressed",
            "Normal again"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(2, "MD013"));
        Assert.IsTrue(map.IsRuleSuppressed(2, "MD033"));
        Assert.IsFalse(map.IsRuleSuppressed(2, "MD001"));
        Assert.IsFalse(map.IsRuleSuppressed(3, "MD013"));
    }

    [TestMethod]
    public void WhenDisableNextLineAtEndOfFileThenNoException()
    {
        var lines = new[]
        {
            "Normal line",
            "<!-- markdownlint-disable-next-line -->"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.AreAllRulesSuppressed(0));
        Assert.IsFalse(map.AreAllRulesSuppressed(1));
    }

    #endregion

    #region capture/restore tests

    [TestMethod]
    public void WhenCaptureRestoreThenStateIsPreserved()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable MD001 -->",
            "MD001 suppressed",
            "<!-- markdownlint-capture -->",
            "<!-- markdownlint-disable MD009 -->",
            "Both MD001 and MD009 suppressed",
            "<!-- markdownlint-restore -->",
            "Only MD001 suppressed again",
            "<!-- markdownlint-enable -->",
            "Nothing suppressed"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"));
        Assert.IsFalse(map.IsRuleSuppressed(1, "MD009"));
        Assert.IsTrue(map.IsRuleSuppressed(4, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(4, "MD009"));
        Assert.IsTrue(map.IsRuleSuppressed(6, "MD001"), "MD001 should be restored");
        Assert.IsFalse(map.IsRuleSuppressed(6, "MD009"), "MD009 should not be present after restore");
        Assert.IsFalse(map.IsRuleSuppressed(8, "MD001"));
    }

    [TestMethod]
    public void WhenRestoreWithoutCaptureThenResetsToDefault()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable MD001 -->",
            "MD001 suppressed",
            "<!-- markdownlint-restore -->",
            "Nothing should be suppressed"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"));
        Assert.IsFalse(map.IsRuleSuppressed(3, "MD001"), "MD001 should be cleared after restore with no capture");
    }

    #endregion

    #region disable-file tests

    [TestMethod]
    public void WhenDisableFileThenAllLinesAfterAreSuppressed()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable-file -->",
            "Line 1",
            "Line 2",
            "Line 3"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.AreAllRulesSuppressed(0));
        Assert.IsTrue(map.AreAllRulesSuppressed(1));
        Assert.IsTrue(map.AreAllRulesSuppressed(2));
        Assert.IsTrue(map.AreAllRulesSuppressed(3));
    }

    [TestMethod]
    public void WhenDisableFileWithRulesThenRuleSuppressedForEntireFile()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable-file MD041 -->",
            "This file doesn't need to start with a heading",
            "More content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(0, "MD041"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD041"));
        Assert.IsTrue(map.IsRuleSuppressed(2, "MD041"));
        Assert.IsFalse(map.IsRuleSuppressed(0, "MD001"));
    }

    [TestMethod]
    public void WhenDisableFileInMiddleOfDocumentThenSuppressesAllLines()
    {
        var lines = new[]
        {
            "Line before",
            "<!-- markdownlint-disable-file MD001 -->",
            "Line after"
        };

        SuppressionMap map = _parser.Parse(lines);

        // File-level suppression applies to ALL lines, even those before the comment
        Assert.IsTrue(map.IsRuleSuppressed(0, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(2, "MD001"));
    }

    #endregion

    #region configure-file tests

    [TestMethod]
    public void WhenConfigureFileThenMentionedRulesSuppressed()
    {
        var lines = new[]
        {
            "<!-- markdownlint-configure-file { \"MD041\": false } -->",
            "Content here",
            "More content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(0, "MD041"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD041"));
        Assert.IsTrue(map.IsRuleSuppressed(2, "MD041"));
    }

    #endregion

    #region rule name/alias tests

    [TestMethod]
    public void WhenDisableByRuleNameThenRuleSuppressed()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable heading-increment -->",
            "Content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(1, null, "heading-increment"));
    }

    [TestMethod]
    public void WhenDisableWithMixedCaseThenRuleSuppressed()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable md001 -->",
            "Content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"));
    }

    [TestMethod]
    public void WhenDisableWithCommaSeparatedRulesThenAllSuppressed()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable MD001, MD009, MD013 -->",
            "Content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD009"));
        Assert.IsTrue(map.IsRuleSuppressed(1, "MD013"));
    }

    #endregion

    #region edge cases

    [TestMethod]
    public void WhenEmptyLinesThenNoException()
    {
        var lines = Array.Empty<string>();

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.HasSuppressions);
    }

    [TestMethod]
    public void WhenNoSuppressionCommentsThenNoSuppressions()
    {
        var lines = new[]
        {
            "# Title",
            "Normal content",
            "<!-- Regular HTML comment -->",
            "More content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.HasSuppressions);
    }

    [TestMethod]
    public void WhenMalformedCommentThenIgnored()
    {
        var lines = new[]
        {
            "<!-- markdownlint-invalid -->",
            "<!-- markdownlint disable -->", // missing hyphen
            "Content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsFalse(map.HasSuppressions);
    }

    [TestMethod]
    public void WhenCommentHasExtraWhitespaceThenStillParsed()
    {
        var lines = new[]
        {
            "<!--   markdownlint-disable   MD001   -->",
            "Content"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(1, "MD001"));
    }

    [TestMethod]
    public void WhenNestedCaptureRestoreThenWorksCorrectly()
    {
        var lines = new[]
        {
            "<!-- markdownlint-disable MD001 -->",
            "MD001 suppressed",
            "<!-- markdownlint-capture -->",
            "<!-- markdownlint-disable MD009 -->",
            "Both suppressed",
            "<!-- markdownlint-capture -->",
            "<!-- markdownlint-disable MD013 -->",
            "All three suppressed",
            "<!-- markdownlint-restore -->",
            "MD001 and MD009 suppressed",
            "<!-- markdownlint-restore -->",
            "Only MD001 suppressed"
        };

        SuppressionMap map = _parser.Parse(lines);

        Assert.IsTrue(map.IsRuleSuppressed(7, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(7, "MD009"));
        Assert.IsTrue(map.IsRuleSuppressed(7, "MD013"));
        Assert.IsTrue(map.IsRuleSuppressed(9, "MD001"));
        Assert.IsTrue(map.IsRuleSuppressed(9, "MD009"));
        Assert.IsFalse(map.IsRuleSuppressed(9, "MD013"));
        Assert.IsTrue(map.IsRuleSuppressed(11, "MD001"));
        Assert.IsFalse(map.IsRuleSuppressed(11, "MD009"));
    }

    #endregion
}
