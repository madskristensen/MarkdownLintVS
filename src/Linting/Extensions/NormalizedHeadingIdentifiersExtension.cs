using System.Collections.Generic;
using System.IO;
using System.Linq;
using Markdig;
using Markdig.Helpers;
using Markdig.Parsers;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace MarkdownLintVS.Linting.Extensions
{
    internal sealed class NormalizedHeadingIdentifiersExtension : IMarkdownExtension
    {
        private static readonly object _normalizedIdKey = new();
        private static readonly object _changedHeadingsKey = new();

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            pipeline.BlockParsers.Find<HeadingBlockParser>().Closed += HeadingClosed;
            pipeline.BlockParsers.FindExact<ParagraphBlockParser>().Closed += HeadingClosed;
            pipeline.DocumentProcessed += NormalizeIdentifiers;
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        private static void HeadingClosed(BlockProcessor processor, Block block)
        {
            if (block is HeadingBlock heading)
            {
                heading.ProcessInlinesEnd += CaptureNormalizedIdentifier;
            }
        }

        private static void CaptureNormalizedIdentifier(InlineProcessor processor, Inline inline)
        {
            HeadingBlock heading = (HeadingBlock)processor.Block;
            HtmlAttributes attributes = heading.TryGetAttributes();
            if (attributes?.Id != null ||
                attributes?.Properties?.Any(property => string.Equals(property.Key, "id", StringComparison.OrdinalIgnoreCase)) == true ||
                heading.Inline == null ||
                !heading.Inline.Descendants<HtmlInline>().Any())
            {
                return;
            }

            using StringWriter writer = new();
            HtmlRenderer renderer = new(writer)
            {
                EnableHtmlForInline = false,
                EnableHtmlEscape = false
            };
            renderer.Render(heading.Inline);
            string text = writer.ToString();

            int end = text.Length - 1;
            int trailingSpaces = 0;
            while (end >= 0)
            {
                char character = text[end];
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                {
                    break;
                }

                if (character == ' ')
                {
                    trailingSpaces++;
                }

                end--;
            }

            if (end >= 0 && trailingSpaces > 0)
            {
                string normalizedId = LinkHelper.UrilizeAsGfm(text.Substring(0, end + 1));
                heading.SetData(_normalizedIdKey, normalizedId);
                if (processor.Document.GetData(_changedHeadingsKey) is not List<HeadingBlock> changedHeadings)
                {
                    changedHeadings = [];
                    processor.Document.SetData(_changedHeadingsKey, changedHeadings);
                }

                changedHeadings.Add(heading);
            }
        }

        private static void NormalizeIdentifiers(MarkdownDocument document)
        {
            if (document.GetData(_changedHeadingsKey) is not List<HeadingBlock> changedHeadings)
            {
                return;
            }

            document.RemoveData(_changedHeadingsKey);
            HashSet<string> identifiers = new(StringComparer.Ordinal);
            foreach (MarkdownObject node in document.Descendants())
            {
                HtmlAttributes attributes = node.TryGetAttributes();
                if (node is not HeadingBlock heading || heading.GetData(_normalizedIdKey) is not string)
                {
                    if (attributes?.Id is string id)
                    {
                        identifiers.Add(id);
                    }
                }

                if (attributes?.Properties != null)
                {
                    foreach (KeyValuePair<string, string> property in attributes.Properties)
                    {
                        if (string.Equals(property.Key, "id", StringComparison.OrdinalIgnoreCase))
                        {
                            identifiers.Add(property.Value);
                        }
                    }
                }
            }

            Dictionary<string, int> suffixes = new(StringComparer.Ordinal);
            foreach (HeadingBlock heading in changedHeadings)
            {
                string baseId = (string)heading.GetData(_normalizedIdKey);
                heading.RemoveData(_normalizedIdKey);
                suffixes.TryGetValue(baseId, out int suffix);
                string id = suffix == 0 ? baseId : $"{baseId}-{suffix}";
                while (!identifiers.Add(id))
                {
                    id = $"{baseId}-{++suffix}";
                }

                suffixes[baseId] = suffix + 1;
                heading.GetAttributes().Id = id;
            }
        }
    }

    internal static class NormalizedHeadingIdentifiersExtensionMethods
    {
        internal static MarkdownPipelineBuilder UseNormalizedHeadingIdentifiers(this MarkdownPipelineBuilder pipeline)
        {
            pipeline.Extensions.AddIfNotAlready<NormalizedHeadingIdentifiersExtension>();
            return pipeline;
        }
    }
}
