using System.Collections.Generic;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Represents a parsed markdown document with helper methods for analysis.
    /// </summary>
    public class MarkdownDocumentAnalysis
    {
        private readonly string _text;
        private readonly string[] _lines;
        private readonly MarkdownDocument _document;
        private readonly MarkdownPipeline _pipeline;

        public string Text => _text;
        public string[] Lines => _lines;
        public MarkdownDocument Document => _document;
        public int LineCount => _lines.Length;

        public MarkdownDocumentAnalysis(string text)
        {
            _text = text ?? string.Empty;
            _lines = SplitLines(_text);

            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UsePreciseSourceLocation()
                .Build();

            _document = Markdown.Parse(_text, _pipeline);
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return [string.Empty];

            var lines = new List<string>();
            var start = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    var end = i;
                    if (end > start && text[end - 1] == '\r')
                        end--;
                    lines.Add(text.Substring(start, end - start));
                    start = i + 1;
                }
            }

            // Add the last line
            if (start <= text.Length)
            {
                var end = text.Length;
                if (end > start && text[end - 1] == '\r')
                    end--;
                lines.Add(text.Substring(start, end - start));
            }

            return [.. lines];
        }

        public string GetLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= _lines.Length)
                return string.Empty;
            return _lines[lineNumber];
        }

        public IEnumerable<HeadingBlock> GetHeadings()
        {
            return _document.Descendants<HeadingBlock>();
        }

        public IEnumerable<FencedCodeBlock> GetFencedCodeBlocks()
        {
            return _document.Descendants<FencedCodeBlock>();
        }

        public IEnumerable<CodeBlock> GetCodeBlocks()
        {
            return _document.Descendants<CodeBlock>();
        }

        public IEnumerable<ListBlock> GetLists()
        {
            return _document.Descendants<ListBlock>();
        }

        public IEnumerable<ListItemBlock> GetListItems()
        {
            return _document.Descendants<ListItemBlock>();
        }

        public IEnumerable<QuoteBlock> GetBlockQuotes()
        {
            return _document.Descendants<QuoteBlock>();
        }

        public IEnumerable<ThematicBreakBlock> GetThematicBreaks()
        {
            return _document.Descendants<ThematicBreakBlock>();
        }

        public IEnumerable<LinkInline> GetLinks()
        {
            return _document.Descendants<LinkInline>();
        }

        public IEnumerable<EmphasisInline> GetEmphasis()
        {
            return _document.Descendants<EmphasisInline>();
        }

        public IEnumerable<CodeInline> GetCodeSpans()
        {
            return _document.Descendants<CodeInline>();
        }

        public IEnumerable<HtmlBlock> GetHtmlBlocks()
        {
            return _document.Descendants<HtmlBlock>();
        }

        public IEnumerable<HtmlInline> GetHtmlInlines()
        {
            return _document.Descendants<HtmlInline>();
        }

        public IEnumerable<Markdig.Extensions.Tables.Table> GetTables()
        {
            return _document.Descendants<Markdig.Extensions.Tables.Table>();
        }

        public IEnumerable<LinkReferenceDefinition> GetLinkReferenceDefinitions()
        {
            return _document.Descendants<LinkReferenceDefinition>();
        }

        public bool IsLineInCodeBlock(int lineNumber)
        {
            foreach (CodeBlock codeBlock in _document.Descendants<CodeBlock>())
            {
                if (lineNumber >= codeBlock.Line && lineNumber <= GetBlockEndLine(codeBlock))
                    return true;
            }
            return false;
        }

        public bool IsLineInHtmlBlock(int lineNumber)
        {
            foreach (HtmlBlock htmlBlock in _document.Descendants<HtmlBlock>())
            {
                if (lineNumber >= htmlBlock.Line && lineNumber <= GetBlockEndLine(htmlBlock))
                    return true;
            }
            return false;
        }

        public bool IsLineInFrontMatter(int lineNumber)
        {
            // Check for YAML front matter (starts with --- on line 0)
            if (_lines.Length > 0 && _lines[0].Trim() == "---")
            {
                for (var i = 1; i < _lines.Length; i++)
                {
                    if (_lines[i].Trim() == "---" || _lines[i].Trim() == "...")
                    {
                        return lineNumber >= 0 && lineNumber <= i;
                    }
                }
            }
            return false;
        }

        public int GetBlockEndLine(Block block)
        {
            if (block.Span.End < 0)
                return block.Line;

            var pos = 0;
            var line = 0;
            while (pos < _text.Length && pos < block.Span.End)
            {
                if (_text[pos] == '\n')
                    line++;
                pos++;
            }
            return line;
        }

        public (int Line, int Column) GetPositionFromOffset(int offset)
        {
            var line = 0;
            var column = 0;

            for (var i = 0; i < Math.Min(offset, _text.Length); i++)
            {
                if (_text[i] == '\n')
                {
                    line++;
                    column = 0;
                }
                else
                {
                    column++;
                }
            }

            return (line, column);
        }

        public int GetOffsetFromPosition(int line, int column)
        {
            var offset = 0;
            var currentLine = 0;

            while (currentLine < line && offset < _text.Length)
            {
                if (_text[offset] == '\n')
                    currentLine++;
                offset++;
            }

            return offset + column;
        }

        public bool IsBlankLine(int lineNumber)
        {
            if (lineNumber < 0 || lineNumber >= _lines.Length)
                return true;
            return string.IsNullOrWhiteSpace(_lines[lineNumber]);
        }

        public int GetFirstNonBlankLine()
        {
            for (var i = 0; i < _lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(_lines[i]))
                    return i;
            }
            return -1;
        }

        public bool EndsWithNewline()
        {
            return _text.Length > 0 && _text[_text.Length - 1] == '\n';
        }

        public bool EndsWithMultipleNewlines()
        {
            if (_text.Length < 2)
                return false;

            var newlineCount = 0;
            for (var i = _text.Length - 1; i >= 0; i--)
            {
                if (_text[i] == '\n')
                    newlineCount++;
                else if (_text[i] != '\r')
                    break;
            }
            return newlineCount > 1;
        }
    }
}
