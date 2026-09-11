using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class AdditionalCodeFixTests
{
    [TestMethod]
    public void MD005_WhenIndentIsInconsistentThenProvidesCorrectedLine()
    {
        var rule = new MD005_ListIndent();
        var analysis = new MarkdownDocumentAnalysis("- one\n - two");

        LintViolation violation = rule.Analyze(analysis, new(), DiagnosticSeverity.Warning).Single();

        Assert.AreEqual("- two", violation.ReplacementText);
    }

    [TestMethod]
    public void MD007_WhenIndentIsWrongThenProvidesCorrectedLine()
    {
        var rule = new MD007_UlIndent();
        var analysis = new MarkdownDocumentAnalysis("- one\n   - two");

        LintViolation violation = rule.Analyze(analysis, new(), DiagnosticSeverity.Warning).Single();

        Assert.AreEqual("  - two", violation.ReplacementText);
    }

    [TestMethod]
    public void MD051_WhenFragmentCanBeNormalizedThenProvidesReplacement()
    {
        var rule = new MD051_LinkFragments();
        var analysis = new MarkdownDocumentAnalysis("# My Heading\n\n[go](#My%20Heading)");

        LintViolation violation = rule.Analyze(analysis, new(), DiagnosticSeverity.Warning).Single();

        Assert.AreEqual("[go](#my-heading)", violation.ReplacementText);
    }

    [TestMethod]
    public void MD053_WhenDefinitionIsUnusedThenProvidesRemoval()
    {
        var rule = new MD053_LinkImageReferenceDefinitions();
        var analysis = new MarkdownDocumentAnalysis("[unused]: https://example.com");

        LintViolation violation = rule.Analyze(analysis, new(), DiagnosticSeverity.Warning).Single();

        Assert.AreEqual(string.Empty, violation.ReplacementText);
    }

    [DataTestMethod]
    [DataRow("[label][]", "collapsed", "shortcut", "[label]")]
    [DataRow("[label]", "shortcut", "full", "[label][label]")]
    [DataRow("[label][label]", "full", "collapsed", "[label][]")]
    [DataRow("<https://example.com>", "autolink", "inline", "[https://example.com](https://example.com)")]
    public void MD054_WhenConversionIsSafeThenReturnsReplacement(
        string text,
        string currentStyle,
        string targetStyle,
        string expected)
    {
        string result = MD054_LinkImageStyle.TryConvertStyle(text, currentStyle, targetStyle);

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void MD054_WhenFullReferenceUsesDifferentLabelThenDoesNotOfferUnsafeFix()
    {
        string result = MD054_LinkImageStyle.TryConvertStyle(
            "[text][reference]",
            "full",
            "collapsed");

        Assert.IsNull(result);
    }
}
