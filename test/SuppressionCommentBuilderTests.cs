using MarkdownLintVS.Commands;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class SuppressionCommentBuilderTests
{
    [TestMethod]
    public void WhenBuildingNewCommentThenCorrectFormat()
    {
        var result = SuppressionCommentBuilder.BuildSuppressionComment("MD001");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 -->", result);
    }

    [TestMethod]
    public void WhenBuildingCommentWithMultipleRulesThenAllIncluded()
    {
        var result = SuppressionCommentBuilder.BuildSuppressionComment("MD001", "MD009", "MD013");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 MD009 MD013 -->", result);
    }

    [TestMethod]
    public void WhenAppendingToExistingCommentWithNoRulesThenRuleAdded()
    {
        var existingComment = "<!-- markdownlint-disable-line -->";

        var result = SuppressionCommentBuilder.AppendRuleToComment(existingComment, "MD001");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 -->", result);
    }

    [TestMethod]
    public void WhenAppendingToExistingCommentWithOneRuleThenRuleAppended()
    {
        var existingComment = "<!-- markdownlint-disable-line MD001 -->";

        var result = SuppressionCommentBuilder.AppendRuleToComment(existingComment, "MD009");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 MD009 -->", result);
    }

    [TestMethod]
    public void WhenAppendingToExistingCommentWithMultipleRulesThenRuleAppended()
    {
        var existingComment = "<!-- markdownlint-disable-line MD001 MD009 -->";

        var result = SuppressionCommentBuilder.AppendRuleToComment(existingComment, "MD013");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 MD009 MD013 -->", result);
    }

    [TestMethod]
    public void WhenAppendingDuplicateRuleThenNotDuplicated()
    {
        var existingComment = "<!-- markdownlint-disable-line MD001 MD009 -->";

        var result = SuppressionCommentBuilder.AppendRuleToComment(existingComment, "MD001");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 MD009 -->", result);
    }

    [TestMethod]
    public void WhenAppendingRuleCaseInsensitiveThenNotDuplicated()
    {
        var existingComment = "<!-- markdownlint-disable-line MD001 -->";

        var result = SuppressionCommentBuilder.AppendRuleToComment(existingComment, "md001");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 -->", result);
    }

    [TestMethod]
    public void WhenExistingCommentHasExtraSpacesThenNormalized()
    {
        var existingComment = "<!--  markdownlint-disable-line   MD001   -->";

        var result = SuppressionCommentBuilder.AppendRuleToComment(existingComment, "MD009");

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 MD009 -->", result);
    }

    [TestMethod]
    public void WhenLineNeedsSuppressionCommentThenSpacingCorrect()
    {
        var lineWithContent = "Some content here";
        var emptyLine = "";
        var whitespaceOnlyLine = "   ";

        Assert.AreEqual(" <!-- markdownlint-disable-line MD001 -->",
            SuppressionCommentBuilder.BuildSuppressionCommentForLine(lineWithContent, "MD001"));

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 -->",
            SuppressionCommentBuilder.BuildSuppressionCommentForLine(emptyLine, "MD001"));

        Assert.AreEqual("<!-- markdownlint-disable-line MD001 -->",
            SuppressionCommentBuilder.BuildSuppressionCommentForLine(whitespaceOnlyLine, "MD001"));
    }

    [TestMethod]
    public void WhenCheckingIfLineHasSuppressionCommentThenCorrectResult()
    {
        Assert.IsTrue(SuppressionCommentBuilder.HasSuppressionComment("text <!-- markdownlint-disable-line --> more"));
        Assert.IsTrue(SuppressionCommentBuilder.HasSuppressionComment("<!-- markdownlint-disable-line MD001 -->"));
        Assert.IsTrue(SuppressionCommentBuilder.HasSuppressionComment("<!-- MARKDOWNLINT-DISABLE-LINE -->"));
        Assert.IsFalse(SuppressionCommentBuilder.HasSuppressionComment("text <!-- markdownlint-disable -->"));
        Assert.IsFalse(SuppressionCommentBuilder.HasSuppressionComment("normal line"));
    }

    [TestMethod]
    public void WhenExtractingExistingCommentThenCorrectSpan()
    {
        var line = "Some text <!-- markdownlint-disable-line MD001 --> more text";

        var (start, length) = SuppressionCommentBuilder.FindSuppressionCommentSpan(line);

        Assert.AreEqual(10, start);
        Assert.AreEqual(40, length);
        Assert.AreEqual("<!-- markdownlint-disable-line MD001 -->", line.Substring(start, length));
    }
}
