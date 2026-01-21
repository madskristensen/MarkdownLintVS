using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Cached analysis result for a text buffer.
    /// </summary>
    internal class CachedAnalysisResult(int snapshotVersion, IReadOnlyList<LintViolation> violations)
    {
        public int SnapshotVersion { get; } = snapshotVersion;
        public IReadOnlyList<LintViolation> Violations { get; } = violations;
    }

    /// <summary>
    /// Provides shared analysis caching for markdown documents. Both the tagger and error list use this to avoid
    /// duplicate parsing.
    /// </summary>
    [Export(typeof(MarkdownAnalysisCache))]
    public class MarkdownAnalysisCache
    {
        private static readonly object _propertyKey = typeof(MarkdownAnalysisCache);
        private static readonly object _debounceKey = typeof(MarkdownAnalysisCache).FullName + ".Debounce";

        /// <summary>
        /// Delay in milliseconds before analyzing after the last keystroke.
        /// </summary>
        private const int _debounceDelayMs = 300;

        /// <summary>
        /// Event raised when analysis results are updated for a buffer.
        /// </summary>
        public event EventHandler<AnalysisUpdatedEventArgs> AnalysisUpdated;

        /// <summary>
        /// Gets cached violations for a buffer, or analyzes if cache is stale.
        /// </summary>
        public IReadOnlyList<LintViolation> GetOrAnalyze(ITextBuffer buffer, string filePath)
        {
            // Return empty if linting is disabled
            if (!GeneralOptions.Instance.LintingEnabled)
            {
                return [];
            }

            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            var version = snapshot.Version.VersionNumber;

            // Check for cached result
            if (buffer.Properties.TryGetProperty(_propertyKey, out CachedAnalysisResult cached) &&
                cached.SnapshotVersion == version)
            {
                return cached.Violations;
            }

            // Analyze and cache
            var text = snapshot.GetText();
            var violations = MarkdownLintAnalyzer.Instance.Analyze(text, filePath).ToList();
            var result = new CachedAnalysisResult(version, violations);

            buffer.Properties[_propertyKey] = result;

            return violations;
        }

        /// <summary>
        /// Triggers immediate analysis on a background thread and notifies all listeners. Use this for initial file
        /// open or when options change.
        /// </summary>
        public void AnalyzeImmediate(ITextBuffer buffer, string filePath)
        {
            // Cancel any pending debounced analysis
            CancelPendingAnalysis(buffer);

            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            var text = snapshot.GetText();

            ThreadHelper.JoinableTaskFactory.StartOnIdle(() => PerformAnalysis(buffer, snapshot, text, filePath)).FireAndForget();
        }

        /// <summary>
        /// Triggers debounced analysis on a background thread. Waits for a pause in typing before analyzing to reduce
        /// CPU usage. Use this when the buffer content changes during editing.
        /// </summary>
        public void InvalidateAndAnalyze(ITextBuffer buffer, string filePath)
        {
            // Cancel any pending analysis for this buffer
            CancelPendingAnalysis(buffer);

            var cts = new CancellationTokenSource();
            buffer.Properties[_debounceKey] = cts;

            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            var text = snapshot.GetText();

            PerformAnalysisAsync(buffer, filePath, cts, snapshot, text).FireAndForget();
        }

        private async Task PerformAnalysisAsync(ITextBuffer buffer, string filePath, CancellationTokenSource cts, ITextSnapshot snapshot, string text)
        {
            try
            {
                await Task.Delay(_debounceDelayMs, cts.Token);

                if (!cts.Token.IsCancellationRequested)
                {
                    PerformAnalysis(buffer, snapshot, text, filePath);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when user types again before delay expires
            }
        }

        /// <summary>
        /// Performs the actual analysis and updates the cache.
        /// </summary>
        private void PerformAnalysis(ITextBuffer buffer, ITextSnapshot snapshot, string text, string filePath)
        {
            try
            {
                // Return empty violations if linting is disabled
                List<LintViolation> violations;
                if (GeneralOptions.Instance.LintingEnabled)
                {
                    violations = MarkdownLintAnalyzer.Instance.Analyze(text, filePath).ToList();
                }
                else
                {
                    violations = [];
                }

                var result = new CachedAnalysisResult(snapshot.Version.VersionNumber, violations);

                buffer.Properties[_propertyKey] = result;

                AnalysisUpdated?.Invoke(this, new AnalysisUpdatedEventArgs(buffer, snapshot, violations, filePath));
            }
            catch (Exception ex)
            {
                ex.Log("Shared markdown analysis failed");
            }
        }

        /// <summary>
        /// Cancels any pending debounced analysis for the buffer.
        /// </summary>
        private void CancelPendingAnalysis(ITextBuffer buffer)
        {
            if (buffer.Properties.TryGetProperty(_debounceKey, out CancellationTokenSource existingCts))
            {
                existingCts.Cancel();
                existingCts.Dispose();
                _ = buffer.Properties.RemoveProperty(_debounceKey);
            }
        }

        /// <summary>
        /// Clears the cache for a buffer.
        /// </summary>
        public void Invalidate(ITextBuffer buffer)
        {
            CancelPendingAnalysis(buffer);
            _ = buffer.Properties.RemoveProperty(_propertyKey);
        }
    }

    /// <summary>
    /// Event args for analysis completion.
    /// </summary>
    public class AnalysisUpdatedEventArgs(
        ITextBuffer buffer,
        ITextSnapshot snapshot,
        IReadOnlyList<LintViolation> violations,
        string filePath) : EventArgs
    {
        public ITextBuffer Buffer { get; } = buffer;
        public ITextSnapshot Snapshot { get; } = snapshot;
        public IReadOnlyList<LintViolation> Violations { get; } = violations;
        public string FilePath { get; } = filePath;
    }
}
