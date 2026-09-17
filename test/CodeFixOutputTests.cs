using MarkdownLintVS.CodeFixes;
using MarkdownLintVS.CodeFixes.Actions;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class CodeFixOutputTests
{
    [TestMethod]
    public void MD012_FixRemovesEntireExcessBlankRun()
    {
        AssertSingleFix(
            new MD012_NoMultipleBlanks(),
            "A\n\n\n\nB",
            "A\n\nB");
    }

    [TestMethod]
    public void MD044_FixCorrectsProperNameCapitalization()
    {
        var configuration = new RuleConfiguration { Value = "GitHub" };

        AssertSingleFix(
            new MD044_ProperNames(),
            "Use github.",
            "Use GitHub.",
            configuration);
    }

    [TestMethod]
    public void MD051_FixReplacesTheLinkWithoutChangingItsLabel()
    {
        AssertSingleFix(
            new MD051_LinkFragments(),
            "# My Heading\n\n[go](#My%20Heading)",
            "# My Heading\n\n[go](#my-heading)");
    }

    [TestMethod]
    public void MD053_FixRemovesUnusedDefinitionAndItsLineBreak()
    {
        AssertSingleFix(
            new MD053_LinkImageReferenceDefinitions(),
            "[unused]: https://example.com\nContent",
            "Content");
    }

    [TestMethod]
    public void MD054_FixConvertsSafeLinkStyle()
    {
        var configuration = new RuleConfiguration();
        configuration.Parameters["autolink"] = "false";

        AssertSingleFix(
            new MD054_LinkImageStyle(),
            "<https://example.com>",
            "[https://example.com](https://example.com)",
            configuration);
    }

    [TestMethod]
    public void MD047_FixPreservesTheDocumentLineEnding()
    {
        AssertSingleFix(
            new MD047_SingleTrailingNewline(),
            "First\nSecond",
            "First\nSecond\n");
    }

    [TestMethod]
    public void RegistryContainsEveryAdvertisedAutoFix()
    {
        string[] expected =
        [
            "MD004", "MD005", "MD007", "MD009", "MD010", "MD011", "MD012", "MD014",
            "MD018", "MD019", "MD020", "MD021", "MD022", "MD023", "MD026", "MD027",
            "MD028", "MD029", "MD030", "MD031", "MD032", "MD034", "MD035", "MD037",
            "MD038", "MD039", "MD040", "MD044", "MD045", "MD047", "MD048", "MD049",
            "MD050", "MD051", "MD053", "MD054", "MD055", "MD056", "MD058"
        ];

        CollectionAssert.AreEquivalent(expected, FixActionRegistry.RegisteredRuleIds.ToArray());
    }

    [TestMethod]
    public void FixAllForRuleAppliesEveryNonOverlappingEdit()
    {
        var textBuffer = new InMemoryTextBuffer("First   \nSecond   \n");
        var action = new FixAllInDocumentAction(
            textBuffer.Buffer.CurrentSnapshot,
            "MD009",
            "test.md");

        action.Invoke(CancellationToken.None);

        Assert.AreEqual("First\nSecond\n", textBuffer.Text);
    }

    [TestMethod]
    public void FixAllAutoFixableDeduplicatesSharedBlankLineBoundaries()
    {
        var textBuffer = new InMemoryTextBuffer("# Heading\n- item\nParagraph");
        var action = new FixAllAutoFixableAction(
            textBuffer.Buffer.CurrentSnapshot,
            "test.md");

        action.Invoke(CancellationToken.None);

        Assert.AreEqual("# Heading\n\n- item\n\nParagraph\n", textBuffer.Text);
    }

    private static void AssertSingleFix(
        IMarkdownRule rule,
        string markdown,
        string expected,
        RuleConfiguration? configuration = null)
    {
        var textBuffer = new InMemoryTextBuffer(markdown);
        var snapshot = textBuffer.Buffer.CurrentSnapshot;
        var analysis = new MarkdownDocumentAnalysis(markdown);
        LintViolation violation = rule.Analyze(
            analysis,
            configuration ?? new(),
            DiagnosticSeverity.Warning).Single();
        var line = snapshot.GetLineFromLineNumber(violation.LineNumber);

        MarkdownFixAction? action = FixActionRegistry.CreateFix(violation, snapshot, line);

        Assert.IsNotNull(action, $"No fix action was created for {violation.Rule.Id}.");
        action.Invoke(CancellationToken.None);
        Assert.AreEqual(expected, textBuffer.Text);
    }
}
