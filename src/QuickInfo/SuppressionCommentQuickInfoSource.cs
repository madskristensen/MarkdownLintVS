using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.QuickInfo
{
    /// <summary>
    /// Provides QuickInfo tooltips for rule names in markdownlint suppression comments.
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Markdown Lint Suppression QuickInfo")]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    [Order(Before = "Markdown Lint QuickInfo")]
    internal sealed class SuppressionCommentQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                typeof(SuppressionCommentQuickInfoSource),
                () => new SuppressionCommentQuickInfoSource(textBuffer));
        }
    }

    /// <summary>
    /// QuickInfo source that displays rule information when hovering over rule IDs or names
    /// in markdownlint suppression comments.
    /// </summary>
    internal sealed class SuppressionCommentQuickInfoSource : IAsyncQuickInfoSource
    {
        // Pattern to match markdownlint suppression comments
        private static readonly Regex _suppressionCommentPattern = new(
            @"<!--\s*markdownlint-(disable|enable|disable-line|disable-next-line|disable-file)(?:\s+([^>]+?))?\s*-->",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Pattern to extract individual rule identifiers
        private static readonly Regex _rulePattern = new(
            @"\b(MD\d{3}|[a-zA-Z][a-zA-Z0-9_-]+)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ITextBuffer _textBuffer;
        private bool _isDisposed;

        public SuppressionCommentQuickInfoSource(ITextBuffer textBuffer)
        {
            _textBuffer = textBuffer;
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                return Task.FromResult<QuickInfoItem>(null);

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(_textBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
                return Task.FromResult<QuickInfoItem>(null);

            // Get the current line
            ITextSnapshotLine line = triggerPoint.Value.GetContainingLine();
            var lineText = line.GetText();

            // Check if we're in a markdownlint suppression comment
            Match commentMatch = _suppressionCommentPattern.Match(lineText);
            if (!commentMatch.Success)
                return Task.FromResult<QuickInfoItem>(null);

            // Get the rules portion of the comment
            if (!commentMatch.Groups[2].Success)
                return Task.FromResult<QuickInfoItem>(null);

            var rulesText = commentMatch.Groups[2].Value;
            var rulesStartInLine = commentMatch.Groups[2].Index;

            // Find which rule the cursor is on
            var cursorPositionInLine = triggerPoint.Value.Position - line.Start.Position;

            foreach (Match ruleMatch in _rulePattern.Matches(rulesText))
            {
                var ruleStartInLine = rulesStartInLine + ruleMatch.Index;
                var ruleEndInLine = ruleStartInLine + ruleMatch.Length;

                if (cursorPositionInLine >= ruleStartInLine && cursorPositionInLine <= ruleEndInLine)
                {
                    var ruleIdOrName = ruleMatch.Value;
                    RuleInfo rule = RuleRegistry.GetRule(ruleIdOrName);

                    if (rule != null)
                    {
                        // Create the QuickInfo content
                        var content = CreateQuickInfoContent(rule);
                        var ruleSpan = new SnapshotSpan(
                            _textBuffer.CurrentSnapshot,
                            line.Start.Position + ruleStartInLine,
                            ruleMatch.Length);

                        ITrackingSpan trackingSpan = _textBuffer.CurrentSnapshot.CreateTrackingSpan(
                            ruleSpan,
                            SpanTrackingMode.EdgeInclusive);

                        return Task.FromResult(new QuickInfoItem(trackingSpan, content));
                    }
                }
            }

            return Task.FromResult<QuickInfoItem>(null);
        }

        private ContainerElement CreateQuickInfoContent(RuleInfo rule)
        {
            var elements = new List<object>();

            // First line: Rule ID (hyperlink) and Rule name on same line, using Text style for blue links
            var headerElement = new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.Text,
                    rule.Id,
                    () => OpenDocumentation(rule.DocumentationUrl),
                    rule.DocumentationUrl),
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.Text,
                    " " + rule.Name));

            elements.Add(headerElement);

            // Description on separate line
            var descriptionElement = new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.NaturalLanguage,
                    rule.Description));

            elements.Add(descriptionElement);

            return new ContainerElement(ContainerElementStyle.Stacked, elements);
        }

        private void OpenDocumentation(string url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Ignore errors opening URL
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
