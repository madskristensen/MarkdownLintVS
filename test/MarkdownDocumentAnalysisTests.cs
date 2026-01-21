using MarkdownLintVS.Linting;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownDocumentAnalysisTests
{
    [TestMethod]
    public void WhenEmptyTextThenLineCountIsOne()
    {
        var analysis = new MarkdownDocumentAnalysis("");

        Assert.AreEqual(1, analysis.LineCount);
    }

    [TestMethod]
    public void WhenSingleLineThenLineCountIsOne()
    {
        var analysis = new MarkdownDocumentAnalysis("Hello World");

        Assert.AreEqual(1, analysis.LineCount);
    }

    [TestMethod]
    public void WhenMultipleLinesThenCorrectLineCount()
    {
        var analysis = new MarkdownDocumentAnalysis("Line 1\nLine 2\nLine 3");

        Assert.AreEqual(3, analysis.LineCount);
    }

    [TestMethod]
    public void WhenWindowsLineEndingsThenCorrectLineCount()
    {
        var analysis = new MarkdownDocumentAnalysis("Line 1\r\nLine 2\r\nLine 3");

        Assert.AreEqual(3, analysis.LineCount);
    }

    [TestMethod]
    public void WhenGetLineThenReturnsCorrectLine()
    {
        var analysis = new MarkdownDocumentAnalysis("Line 0\nLine 1\nLine 2");

        Assert.AreEqual("Line 0", analysis.GetLine(0));
        Assert.AreEqual("Line 1", analysis.GetLine(1));
        Assert.AreEqual("Line 2", analysis.GetLine(2));
    }

    [TestMethod]
    public void WhenGetLineWithInvalidIndexThenReturnsEmpty()
    {
        var analysis = new MarkdownDocumentAnalysis("Single line");

        Assert.AreEqual(string.Empty, analysis.GetLine(-1));
        Assert.AreEqual(string.Empty, analysis.GetLine(5));
    }

    [TestMethod]
    public void WhenDocumentHasHeadingsThenGetHeadingsReturnsAll()
    {
        var markdown = "# Heading 1\n\nSome text\n\n## Heading 2\n\nMore text\n\n### Heading 3";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var headings = analysis.GetHeadings().ToList();

        Assert.HasCount(3, headings);
        Assert.AreEqual(1, headings[0].Level);
        Assert.AreEqual(2, headings[1].Level);
        Assert.AreEqual(3, headings[2].Level);
    }

    [TestMethod]
    public void WhenDocumentHasFencedCodeBlockThenGetFencedCodeBlocksReturnsIt()
    {
        var markdown = "# Title\n\n```csharp\nvar x = 1;\n```\n\nText";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var codeBlocks = analysis.GetFencedCodeBlocks().ToList();

        Assert.HasCount(1, codeBlocks);
    }

    [TestMethod]
    public void WhenDocumentHasListsThenGetListsReturnsAll()
    {
        var markdown = "# Title\n\n- Item 1\n- Item 2\n\n1. First\n2. Second";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var lists = analysis.GetLists().ToList();

        Assert.HasCount(2, lists);
    }

    [TestMethod]
    public void WhenDocumentHasBlockQuotesThenGetBlockQuotesReturnsAll()
    {
        var markdown = "> Quote 1\n\nText\n\n> Quote 2";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var quotes = analysis.GetBlockQuotes().ToList();

        Assert.HasCount(2, quotes);
    }

    [TestMethod]
    public void WhenLineInCodeBlockThenIsLineInCodeBlockReturnsTrue()
    {
        var markdown = "Text\n\n```\ncode line\n```\n\nMore text";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsFalse(analysis.IsLineInCodeBlock(0)); // Text
        Assert.IsTrue(analysis.IsLineInCodeBlock(2));  // ```
        Assert.IsTrue(analysis.IsLineInCodeBlock(3));  // code line
        Assert.IsTrue(analysis.IsLineInCodeBlock(4));  // ```
        Assert.IsFalse(analysis.IsLineInCodeBlock(6)); // More text
    }

    [TestMethod]
    public void WhenDocumentHasFrontMatterThenIsLineInFrontMatterReturnsTrue()
    {
        var markdown = "---\ntitle: Test\nauthor: Me\n---\n\n# Content";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsTrue(analysis.IsLineInFrontMatter(0));  // ---
        Assert.IsTrue(analysis.IsLineInFrontMatter(1));  // title
        Assert.IsTrue(analysis.IsLineInFrontMatter(2));  // author
        Assert.IsTrue(analysis.IsLineInFrontMatter(3));  // ---
        Assert.IsFalse(analysis.IsLineInFrontMatter(5)); // # Content
    }

    [TestMethod]
    public void WhenNoFrontMatterThenIsLineInFrontMatterReturnsFalse()
    {
        var markdown = "# Title\n\nContent";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsFalse(analysis.IsLineInFrontMatter(0));
        Assert.IsFalse(analysis.IsLineInFrontMatter(1));
    }

    [TestMethod]
    public void WhenFrontMatterHasTitleThenHasFrontMatterTitleReturnsTrue()
    {
        var markdown = "---\ntitle: My Document\n---\n\n# Content";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsTrue(analysis.HasFrontMatterTitle());
    }

    [TestMethod]
    public void WhenFrontMatterHasNoTitleThenHasFrontMatterTitleReturnsFalse()
    {
        var markdown = "---\ndescription: Something\nauthor: Me\n---\n\n# Content";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsFalse(analysis.HasFrontMatterTitle());
    }

    [TestMethod]
    public void WhenNoFrontMatterThenHasFrontMatterTitleReturnsFalse()
    {
        var markdown = "# Title\n\nContent";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsFalse(analysis.HasFrontMatterTitle());
    }

    [TestMethod]
    public void WhenCustomPatternMatchesThenHasFrontMatterTitleReturnsTrue()
    {
        var markdown = "---\nname: My Document\n---\n\n# Content";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.IsTrue(analysis.HasFrontMatterTitle(@"^\s*name\s*:"));
        Assert.IsFalse(analysis.HasFrontMatterTitle(@"^\s*title\s*[:=]"));
    }

    [TestMethod]
    public void WhenDocumentHasLinksThenGetLinksReturnsAll()
    {
        var markdown = "[Link 1](http://example.com)\n\n[Link 2](http://test.com)";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var links = analysis.GetLinks().ToList();

        Assert.HasCount(2, links);
    }

    [TestMethod]
    public void WhenDocumentHasEmphasisThenGetEmphasisReturnsAll()
    {
        var markdown = "This is *italic* and **bold** text";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var emphasis = analysis.GetEmphasis().ToList();

        Assert.HasCount(2, emphasis);
    }

    [TestMethod]
    public void WhenDocumentHasCodeSpansThenGetCodeSpansReturnsAll()
    {
        var markdown = "Use `code1` and `code2` inline";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var codeSpans = analysis.GetCodeSpans().ToList();

        Assert.HasCount(2, codeSpans);
    }

    [TestMethod]
    public void WhenDocumentHasTablesThenGetTablesReturnsAll()
    {
        var markdown = "| Col1 | Col2 |\n|------|------|\n| A    | B    |";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var tables = analysis.GetTables().ToList();

        Assert.HasCount(1, tables);
    }

    [TestMethod]
    public void WhenDocumentHasThematicBreaksThenGetThematicBreaksReturnsAll()
    {
        var markdown = "Section 1\n\n---\n\nSection 2\n\n***\n\nSection 3";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var breaks = analysis.GetThematicBreaks().ToList();

        Assert.HasCount(2, breaks);
    }

    [TestMethod]
    public void WhenDocumentHasLinkReferenceDefinitionsThenGetLinkReferenceDefinitionsReturnsAll()
    {
        var markdown = "[link][label]\n\n[label]: https://example.com\n[other]: https://other.com";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var definitions = analysis.GetLinkReferenceDefinitions().ToList();

        Assert.HasCount(2, definitions);
    }

    [TestMethod]
    public void WhenDocumentHasSingleLinkReferenceDefinitionThenGetLinkReferenceDefinitionsReturnsIt()
    {
        var markdown = "[label]: https://example.com";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var definitions = analysis.GetLinkReferenceDefinitions().ToList();

        Assert.HasCount(1, definitions);
        Assert.AreEqual("label", definitions[0].Label);
    }
}
