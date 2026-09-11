using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.Commands;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Tagging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.QuickInfo
{
    /// <summary>
    /// Provides QuickInfo tooltips for markdown lint errors.
    /// </summary>
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Markdown Lint QuickInfo")]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    [Order(Before = "Default Quick Info Presenter")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal sealed class MarkdownLintQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new MarkdownLintQuickInfoSource(textBuffer));
        }
    }

    /// <summary>
    /// QuickInfo source that displays markdown lint error messages with clickable help links.
    /// </summary>
    internal sealed class MarkdownLintQuickInfoSource(ITextBuffer textBuffer) : IAsyncQuickInfoSource
    {
        private bool _isDisposed;

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
        {
            if (_isDisposed)
                return null;

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(textBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
                return null;

            // Get tagger from buffer properties
            if (!textBuffer.Properties.TryGetProperty(typeof(MarkdownLintTagger), out MarkdownLintTagger tagger))
                return null;

            // Get lint results at the trigger point
            var lintResults = tagger.GetLintResultsAtPoint(triggerPoint.Value).ToList();
            if (lintResults.Count == 0)
                return null;

            string solutionDirectory = await GetSolutionDirectoryAsync(cancellationToken);

            // Build QuickInfo content for all lint results at this position
            var elements = new List<object>();
            foreach (LintResult result in lintResults)
            {
                if (elements.Count > 0)
                {
                    // Add separator between multiple errors
                    elements.Add(new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.WhiteSpace, string.Empty)));
                }

                elements.Add(CreateQuickInfoContent(result, triggerPoint.Value, solutionDirectory));
            }

            // Get the span to track
            SnapshotSpan? applicableSpan = lintResults[0].GetTranslatedSpan(textBuffer.CurrentSnapshot);
            if (!applicableSpan.HasValue)
                return null;

            ITrackingSpan trackingSpan = textBuffer.CurrentSnapshot.CreateTrackingSpan(
                applicableSpan.Value,
                SpanTrackingMode.EdgeInclusive);

            return new QuickInfoItem(
                trackingSpan,
                new ContainerElement(ContainerElementStyle.Stacked, elements));
        }

        private ContainerElement CreateQuickInfoContent(LintResult result, SnapshotPoint triggerPoint, string solutionDirectory)
        {
            // First line: error message (without the rule ID prefix since we show it below)
            var message = result.Message;
            if (message.StartsWith(result.RuleId + ": "))
            {
                message = message.Substring(result.RuleId.Length + 2);
            }

            var messageRun = new ClassifiedTextRun(
                PredefinedClassificationTypeNames.NaturalLanguage,
                message);

            var messageElement = new ClassifiedTextElement(messageRun);

            // Error code as hyperlink
            var linkElement = new ClassifiedTextElement(
                new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.Text,
                    result.RuleId,
                    () => OpenDocumentation(result.DocumentationUrl),
                    result.DocumentationUrl));

            var actionElements = new List<ClassifiedTextRun>
            {
                new(
                    PredefinedClassificationTypeNames.Text,
                    "Suppress in code",
                    () => SuppressError(triggerPoint, result.RuleId),
                    "Suppress this error with an inline comment"),
                new(PredefinedClassificationTypeNames.WhiteSpace, "    ")
            };

            string filePath = GetFilePath();
            if (EditorConfigSuppressionHelper.FindTargetPath(filePath, solutionDirectory) != null)
            {
                actionElements.Add(new ClassifiedTextRun(
                    PredefinedClassificationTypeNames.Text,
                    "Suppress in .editorconfig",
                    () => SuppressInEditorConfig(result.RuleId, filePath, solutionDirectory),
                    "Disable this rule for matching Markdown files"));
            }

            return new ContainerElement(
                ContainerElementStyle.Stacked,
                linkElement, messageElement,
                new ClassifiedTextElement(actionElements));
        }

        private void SuppressError(SnapshotPoint triggerPoint, string ruleId)
        {
            SuppressionHelper.SuppressAtPoint(textBuffer, triggerPoint, ruleId);
        }

        private void SuppressInEditorConfig(string ruleId, string filePath, string solutionDirectory)
        {
            RuleInfo rule = RuleRegistry.GetRule(ruleId);
            if (rule == null || !EditorConfigSuppressionHelper.Suppress(filePath, rule.Name, solutionDirectory))
                return;

            string editorConfigPath = EditorConfigSuppressionHelper.FindTargetPath(filePath, solutionDirectory);
            _ = VS.Documents.OpenAsync(editorConfigPath);
            MarkdownLintAnalyzer.Instance.ClearEditorConfigCache();

            if (textBuffer.Properties.TryGetProperty(typeof(MarkdownLintTagger), out MarkdownLintTagger tagger))
                tagger.Reanalyze();
        }

        private string GetFilePath()
        {
            return textBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document)
                ? document.FilePath
                : null;
        }

        private async Task<string> GetSolutionDirectoryAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            return solution?.FullPath != null ? Path.GetDirectoryName(solution.FullPath) : null;
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
