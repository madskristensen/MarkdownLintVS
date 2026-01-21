using MarkdownLintVS.Linting;

namespace MarkdownLintVS.Test;

/// <summary>
/// Tests for ViolationMessageParser - demonstrating VS-independent testability.
/// These tests require NO Visual Studio dependencies.
/// </summary>
[TestClass]
public sealed class ViolationMessageParserTests
{
    #region ExtractExpectedMarker Tests

    [TestMethod]
    public void ExtractExpectedMarker_WithExpectedDash_ReturnsDash()
    {
        var result = ViolationMessageParser.ExtractExpectedMarker("Inconsistent list marker (expected 'dash')");
        Assert.AreEqual('-', result);
    }

    [TestMethod]
    public void ExtractExpectedMarker_WithShouldUseDash_ReturnsDash()
    {
        var result = ViolationMessageParser.ExtractExpectedMarker("List should use dash marker");
        Assert.AreEqual('-', result);
    }

    [TestMethod]
    public void ExtractExpectedMarker_WithExpectedAsterisk_ReturnsAsterisk()
    {
        var result = ViolationMessageParser.ExtractExpectedMarker("Inconsistent list marker (expected 'asterisk')");
        Assert.AreEqual('*', result);
    }

    [TestMethod]
    public void ExtractExpectedMarker_WithExpectedPlus_ReturnsPlus()
    {
        var result = ViolationMessageParser.ExtractExpectedMarker("Inconsistent list marker (expected 'plus')");
        Assert.AreEqual('+', result);
    }

    [TestMethod]
    public void ExtractExpectedMarker_WithUnknownMessage_ReturnsNull()
    {
        var result = ViolationMessageParser.ExtractExpectedMarker("Some other message");
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ExtractExpectedMarker_WithNullMessage_ReturnsNull()
    {
        var result = ViolationMessageParser.ExtractExpectedMarker(null);
        Assert.IsNull(result);
    }

    #endregion

    #region ExtractExpectedNumber Tests

    [TestMethod]
    public void ExtractExpectedNumber_WithShouldBe1_Returns1()
    {
        var result = ViolationMessageParser.ExtractExpectedNumber("Ordered list item prefix should be '1'");
        Assert.AreEqual(1, result);
    }

    [TestMethod]
    public void ExtractExpectedNumber_WithShouldBe42_Returns42()
    {
        var result = ViolationMessageParser.ExtractExpectedNumber("Ordered list item prefix should be '42'");
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void ExtractExpectedNumber_WithNoNumber_ReturnsNull()
    {
        var result = ViolationMessageParser.ExtractExpectedNumber("Some message without number");
        Assert.IsNull(result);
    }

    #endregion

    #region ExtractExpectedStyle Tests

    [TestMethod]
    public void ExtractExpectedStyle_WithExpectedBacktick_ReturnsBacktick()
    {
        var result = ViolationMessageParser.ExtractExpectedStyle("Code fence style (expected 'backtick')");
        Assert.AreEqual("backtick", result);
    }

    [TestMethod]
    public void ExtractExpectedStyle_WithExpectedColonFormat_ReturnsStyle()
    {
        var result = ViolationMessageParser.ExtractExpectedStyle("Expected: asterisk, Found: underscore");
        Assert.AreEqual("asterisk", result);
    }

    [TestMethod]
    public void ExtractExpectedStyle_WithHorizontalRuleDashes_ReturnsDashes()
    {
        var result = ViolationMessageParser.ExtractExpectedStyle("Horizontal rule style should be ---");
        Assert.AreEqual("---", result);
    }

    [TestMethod]
    public void ExtractExpectedStyle_WithHorizontalRuleAsterisks_ReturnsAsterisks()
    {
        var result = ViolationMessageParser.ExtractExpectedStyle("Horizontal rule style should be ***");
        Assert.AreEqual("***", result);
    }

    #endregion

    #region ExtractBlankLinePosition Tests

    [TestMethod]
    public void ExtractBlankLinePosition_WithBefore_ReturnsBefore()
    {
        var result = ViolationMessageParser.ExtractBlankLinePosition("Add blank line before heading");
        Assert.AreEqual("before", result);
    }

    [TestMethod]
    public void ExtractBlankLinePosition_WithAfter_ReturnsAfter()
    {
        var result = ViolationMessageParser.ExtractBlankLinePosition("Add blank line after heading");
        Assert.AreEqual("after", result);
    }

    [TestMethod]
    public void ExtractBlankLinePosition_WithNeither_ReturnsNull()
    {
        var result = ViolationMessageParser.ExtractBlankLinePosition("Some other message");
        Assert.IsNull(result);
    }

    #endregion

    #region IsMultipleNewlines Tests

    [TestMethod]
    public void IsMultipleNewlines_WithMultiple_ReturnsTrue()
    {
        var result = ViolationMessageParser.IsMultipleNewlines("File ends with multiple newlines");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsMultipleNewlines_WithoutMultiple_ReturnsFalse()
    {
        var result = ViolationMessageParser.IsMultipleNewlines("File should end with newline");
        Assert.IsFalse(result);
    }

    #endregion
}
