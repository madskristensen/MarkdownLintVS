using System.Collections.Generic;
using System.Text;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace MarkdownLintVS.Linting.Extensions
{
    internal sealed class TocTokenExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<TocTokenParser>())
            {
                pipeline.BlockParsers.InsertBefore<ParagraphBlockParser>(new TocTokenParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
            if (renderer is HtmlRenderer htmlRenderer && !htmlRenderer.ObjectRenderers.Contains<TocTokenRenderer>())
            {
                htmlRenderer.ObjectRenderers.Add(new TocTokenRenderer());
            }
        }
    }

    internal sealed class TocToken : LeafBlock
    {
        public TocToken(BlockParser parser) : base(parser)
        {
        }
    }

    internal sealed class TocTokenParser : BlockParser
    {
        private const string _tocMarker = "[[_TOC_]]";

        public TocTokenParser()
        {
            OpeningCharacters = ['['];
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            StringSlice line = processor.Line;
            if (line.ToString().Trim() != _tocMarker)
            {
                return BlockState.None;
            }

            TocToken tocToken = new(this)
            {
                Column = processor.Column,
                Span = new SourceSpan(processor.Start, processor.Start + _tocMarker.Length - 1),
                Line = processor.LineIndex
            };

            processor.NewBlocks.Push(tocToken);
            return BlockState.BreakDiscard;
        }
    }

    internal sealed class TocTokenRenderer : HtmlObjectRenderer<TocToken>
    {
        protected override void Write(HtmlRenderer renderer, TocToken obj)
        {
            MarkdownDocument document = GetRootDocument(obj);
            if (document == null)
            {
                return;
            }

            List<HeadingBlock> headings = [];
            foreach (MarkdownObject descendant in document.Descendants())
            {
                if (descendant is HeadingBlock heading)
                {
                    headings.Add(heading);
                }
            }

            if (headings.Count == 0)
            {
                return;
            }

            StringBuilder builder = new();
            builder.AppendLine("<div class=\"toc\">");
            builder.AppendLine("<p><strong>Contents</strong></p>");
            Dictionary<string, int> headerCounts = new(StringComparer.OrdinalIgnoreCase);
            var currentLevel = 0;

            foreach (HeadingBlock heading in headings)
            {
                string title = GetHeadingText(heading);
                string id = heading.GetAttributes()?.Id;
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (headerCounts.TryGetValue(id, out int count))
                {
                    headerCounts[id] = count + 1;
                    id = $"{id}-{count}";
                }
                else
                {
                    headerCounts[id] = 1;
                }

                while (currentLevel >= heading.Level)
                {
                    builder.AppendLine("</ul>");
                    currentLevel--;
                }

                while (currentLevel < heading.Level)
                {
                    builder.AppendLine("<ul>");
                    currentLevel++;
                }

                builder.AppendLine($"<li><a href=\"#{id}\">{System.Net.WebUtility.HtmlEncode(title)}</a></li>");
            }

            while (currentLevel > 0)
            {
                builder.AppendLine("</ul>");
                currentLevel--;
            }

            builder.AppendLine("</div>");
            renderer.Write(builder.ToString());
        }

        private static MarkdownDocument GetRootDocument(Block block)
        {
            Block current = block;
            while (current != null)
            {
                if (current is MarkdownDocument document)
                {
                    return document;
                }

                current = current.Parent;
            }

            return null;
        }

        private static string GetHeadingText(HeadingBlock heading)
        {
            StringBuilder builder = new();
            if (heading.Inline != null)
            {
                foreach (Markdig.Syntax.Inlines.Inline inline in heading.Inline)
                {
                    ExtractText(inline, builder);
                }
            }

            return builder.ToString().Trim();
        }

        private static void ExtractText(Markdig.Syntax.Inlines.Inline inline, StringBuilder builder)
        {
            if (inline is Markdig.Syntax.Inlines.LiteralInline literal)
            {
                builder.Append(literal.Content.ToString());
            }
            else if (inline is Markdig.Syntax.Inlines.ContainerInline container)
            {
                foreach (Markdig.Syntax.Inlines.Inline child in container)
                {
                    ExtractText(child, builder);
                }
            }
        }
    }

    internal static class TocTokenExtensionMethods
    {
        internal static MarkdownPipelineBuilder UseTocToken(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<TocTokenExtension>();
            return pipeline;
        }
    }
}
