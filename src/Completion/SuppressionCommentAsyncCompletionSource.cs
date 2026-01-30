using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.Completion
{
    /// <summary>
    /// Provides async IntelliSense completions for rule names in markdownlint suppression comments.
    /// Uses the modern IAsyncCompletionSource API which supports filters.
    /// </summary>
    [Export(typeof(IAsyncCompletionSourceProvider))]
    [Name("Markdown Lint Suppression Async Completion")]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    internal sealed class SuppressionCommentAsyncCompletionSourceProvider : IAsyncCompletionSourceProvider
    {
        // Filter definitions with icons
        private static readonly CompletionFilter RuleCodeFilter = new("Rule Codes", "C", new ImageElement(KnownMonikers.Constant.ToImageId()));
        private static readonly CompletionFilter RuleNameFilter = new("Rule Names", "N", new ImageElement(KnownMonikers.Field.ToImageId()));

        internal static ImmutableArray<CompletionFilter> Filters { get; } = ImmutableArray.Create(RuleCodeFilter, RuleNameFilter);

        public IAsyncCompletionSource GetOrCreate(ITextView textView)
        {
            return textView.Properties.GetOrCreateSingletonProperty(
                typeof(SuppressionCommentAsyncCompletionSource),
                () => new SuppressionCommentAsyncCompletionSource(RuleCodeFilter, RuleNameFilter));
        }
    }

    /// <summary>
    /// Async completion source that provides rule IDs and names for markdownlint suppression comments.
    /// </summary>
    internal sealed class SuppressionCommentAsyncCompletionSource : IAsyncCompletionSource
    {
        // Pattern to detect if we're inside a markdownlint suppression comment
        private static readonly Regex _suppressionCommentPattern = new(
            @"<!--\s*markdownlint-(disable|enable|disable-line|disable-next-line|disable-file)(\s|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Pattern to detect the end of the comment
        private static readonly Regex _commentEndPattern = new(
            @"-->",
            RegexOptions.Compiled);

        private readonly CompletionFilter _ruleCodeFilter;
        private readonly CompletionFilter _ruleNameFilter;
        private ImmutableArray<CompletionItem> _cachedItems = ImmutableArray<CompletionItem>.Empty;

        public SuppressionCommentAsyncCompletionSource(CompletionFilter ruleCodeFilter, CompletionFilter ruleNameFilter)
        {
            _ruleCodeFilter = ruleCodeFilter;
            _ruleNameFilter = ruleNameFilter;
        }

        public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        {
            // Don't trigger on deletion
            if (trigger.Reason == CompletionTriggerReason.Deletion)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // Get the current line
            ITextSnapshotLine line = triggerLocation.GetContainingLine();
            var lineText = line.GetText();
            var cursorPositionInLine = triggerLocation.Position - line.Start.Position;

            // Handle edge case
            if (cursorPositionInLine <= 0)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            var textBeforeCursor = lineText.Substring(0, cursorPositionInLine);

            // Check if we're inside a markdownlint suppression comment
            Match startMatch = _suppressionCommentPattern.Match(textBeforeCursor);
            if (!startMatch.Success)
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // Calculate where the directive ends
            var matchEnd = startMatch.Index + startMatch.Length;

            // Check if there's a --> between the directive and cursor
            var textBetweenMatchAndCursor = cursorPositionInLine > matchEnd
                ? lineText.Substring(matchEnd, cursorPositionInLine - matchEnd)
                : string.Empty;

            if (_commentEndPattern.IsMatch(textBetweenMatchAndCursor))
            {
                return CompletionStartData.DoesNotParticipateInCompletion;
            }

            // Find the start of the current word being typed
            var wordStart = cursorPositionInLine;
            while (wordStart > matchEnd &&
                   wordStart > 0 &&
                   !char.IsWhiteSpace(lineText[wordStart - 1]) &&
                   lineText[wordStart - 1] != ',')
            {
                wordStart--;
            }

            // Create the applicable span
            var applicableSpan = new SnapshotSpan(
                triggerLocation.Snapshot,
                line.Start.Position + wordStart,
                cursorPositionInLine - wordStart);

            return new CompletionStartData(CompletionParticipation.ProvidesItems, applicableSpan);
        }

        public Task<CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session,
            CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableToSpan,
            CancellationToken token)
        {
            // Build completion items if not cached
            if (_cachedItems.IsEmpty)
            {
                _cachedItems = BuildCompletionItems();
            }

            return Task.FromResult(new CompletionContext(_cachedItems));
        }

        private ImmutableArray<CompletionItem> BuildCompletionItems()
        {
            var builder = ImmutableArray.CreateBuilder<CompletionItem>();

            // Icons for completion items
            var codeIcon = new ImageElement(KnownMonikers.Constant.ToImageId());
            var nameIcon = new ImageElement(KnownMonikers.Field.ToImageId());

            foreach (RuleInfo rule in RuleRegistry.AllRules.OrderBy(r => r.Id))
            {
                // Add completion for rule ID (e.g., "MD007")
                var idItem = new CompletionItem(
                    displayText: rule.Id,
                    source: this,
                    icon: codeIcon,
                    filters: ImmutableArray.Create(_ruleCodeFilter),
                    suffix: rule.Name,
                    insertText: rule.Id,
                    sortText: rule.Id,
                    filterText: rule.Id,
                    automationText: rule.Id,
                    attributeIcons: ImmutableArray<ImageElement>.Empty);

                idItem.Properties.AddProperty("Description", rule.Description);
                builder.Add(idItem);

                // Add completion for rule name (e.g., "ul-indent")
                var nameItem = new CompletionItem(
                    displayText: rule.Name,
                    source: this,
                    icon: nameIcon,
                    filters: ImmutableArray.Create(_ruleNameFilter),
                    suffix: rule.Id,
                    insertText: rule.Name,
                    sortText: rule.Name,
                    filterText: rule.Name,
                    automationText: rule.Name,
                    attributeIcons: ImmutableArray<ImageElement>.Empty);

                nameItem.Properties.AddProperty("Description", rule.Description);
                builder.Add(nameItem);
            }

            return builder.ToImmutable();
        }

        public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        {
            if (item.Properties.TryGetProperty("Description", out string description))
            {
                return Task.FromResult<object>(description);
            }

            return Task.FromResult<object>(null);
        }
    }
}
