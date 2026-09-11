using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

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

    internal sealed class PendingAnalysis(CancellationTokenSource cancellationTokenSource, int snapshotVersion, bool isDebounced)
    {
        public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
        public int SnapshotVersion { get; } = snapshotVersion;
        public bool IsDebounced { get; } = isDebounced;
    }

    /// <summary>
    /// Provides shared analysis caching for markdown documents. Both the tagger and error list use this to avoid
    /// duplicate parsing.
    /// </summary>
    [Export(typeof(MarkdownAnalysisCache))]
    public class MarkdownAnalysisCache
    {
        private static readonly object _propertyKey = typeof(MarkdownAnalysisCache);
        private static readonly object _pendingAnalysisKey = typeof(MarkdownAnalysisCache).FullName + ".PendingAnalysis";
        private readonly IEditorOptionsFactoryService _editorOptionsFactory;

        /// <summary>
        /// Delay in milliseconds before analyzing after the last keystroke.
        /// </summary>
        private const int _debounceDelayMs = 300;

        /// <summary>
        /// Event raised when analysis results are updated for a buffer.
        /// </summary>
        public event EventHandler<AnalysisUpdatedEventArgs> AnalysisUpdated;

        [ImportingConstructor]
        public MarkdownAnalysisCache(IEditorOptionsFactoryService editorOptionsFactory)
        {
            _editorOptionsFactory = editorOptionsFactory;
        }

        internal IEditorOptions GetEditorOptions(ITextBuffer buffer) => _editorOptionsFactory.GetOptions(buffer);

        /// <summary>
        /// Gets cached violations for a buffer without blocking. Returns cached results even if stale,
        /// and triggers background analysis if the cache is outdated. This avoids blocking the UI thread
        /// during selection changes.
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
            if (buffer.Properties.TryGetProperty(_propertyKey, out CachedAnalysisResult cached))
            {
                if (cached.SnapshotVersion == version)
                {
                    // Cache is current
                    return cached.Violations;
                }

                // Cache is stale - trigger background analysis but return stale results
                // to avoid blocking the UI thread
                AnalyzeImmediate(buffer, filePath);
                return cached.Violations;
            }

            // No cache at all - trigger background analysis and return empty
            AnalyzeImmediate(buffer, filePath);
            return [];
        }

        /// <summary>
        /// Gets violations only when they belong to the current snapshot. Stale locations must not
        /// be used for operations that edit the document, such as suggested actions.
        /// </summary>
        public IReadOnlyList<LintViolation> GetCurrentOrAnalyze(ITextBuffer buffer, string filePath)
        {
            if (!GeneralOptions.Instance.LintingEnabled)
            {
                return [];
            }

            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            if (buffer.Properties.TryGetProperty(_propertyKey, out CachedAnalysisResult cached))
            {
                IReadOnlyList<LintViolation> current = GetViolationsForSnapshot(
                    cached,
                    snapshot.Version.VersionNumber);
                if (current.Count > 0 || cached.SnapshotVersion == snapshot.Version.VersionNumber)
                {
                    return current;
                }
            }

            AnalyzeImmediate(buffer, filePath);
            return [];
        }

        internal static IReadOnlyList<LintViolation> GetViolationsForSnapshot(
            CachedAnalysisResult cached,
            int snapshotVersion)
        {
            return cached?.SnapshotVersion == snapshotVersion ? cached.Violations : [];
        }

        /// <summary>
        /// Triggers analysis without debounce delay on a background thread.
        /// Use this for initial file open or when options change. The snapshot and text are
        /// captured on the calling thread, then analysis runs off the UI thread and notifies
        /// listeners via AnalysisUpdated.
        /// </summary>
        public void AnalyzeImmediate(ITextBuffer buffer, string filePath)
        {
            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            if (HasPendingImmediateAnalysisForSnapshot(buffer, snapshot.Version.VersionNumber))
            {
                return;
            }

            // Cancel any pending debounced analysis
            CancelPendingAnalysis(buffer);

            // Capture text and run analysis on a background thread without debounce delay.
            var pendingAnalysis = new PendingAnalysis(new CancellationTokenSource(), snapshot.Version.VersionNumber, isDebounced: false);
            buffer.Properties[_pendingAnalysisKey] = pendingAnalysis;
            PerformAnalysisNowAsync(buffer, filePath, pendingAnalysis, pendingAnalysis.CancellationTokenSource.Token, snapshot).FireAndForget();
        }

        /// <summary>
        /// Forces a fresh analysis when external configuration changes without changing the buffer snapshot.
        /// </summary>
        public void Reanalyze(ITextBuffer buffer, string filePath)
        {
            CancelPendingAnalysis(buffer);
            _ = buffer.Properties.RemoveProperty(_propertyKey);
            AnalyzeImmediate(buffer, filePath);
        }

        /// <summary>
        /// Triggers debounced analysis on a background thread.
        /// CPU usage. Use this when the buffer content changes during editing.
        /// </summary>
        public void InvalidateAndAnalyze(ITextBuffer buffer, string filePath)
        {
            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            if (HasPendingAnalysisForSnapshot(buffer, snapshot.Version.VersionNumber))
            {
                return;
            }

            // Cancel any pending analysis for this buffer
            CancelPendingAnalysis(buffer);

            var cts = new CancellationTokenSource();
            var pendingAnalysis = new PendingAnalysis(cts, snapshot.Version.VersionNumber, isDebounced: true);
            buffer.Properties[_pendingAnalysisKey] = pendingAnalysis;

            // Pass the token, not the CTS, to avoid accessing disposed CTS.
            PerformAnalysisAsync(buffer, filePath, pendingAnalysis, cts.Token, snapshot).FireAndForget();
        }

        private async Task PerformAnalysisAsync(ITextBuffer buffer, string filePath, PendingAnalysis pendingAnalysis, CancellationToken cancellationToken, ITextSnapshot snapshot)
        {
            try
            {
                await Task.Delay(_debounceDelayMs, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Run(() =>
                    {
                        string text = snapshot.GetText();
                        PerformAnalysis(buffer, snapshot, text, filePath, pendingAnalysis, cancellationToken);
                    }, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when user types again before delay expires
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource was disposed - this is fine, just stop
            }
            finally
            {
                ClearPendingAnalysisIfMatches(buffer, pendingAnalysis);
            }
        }

        private async Task PerformAnalysisNowAsync(ITextBuffer buffer, string filePath, PendingAnalysis pendingAnalysis, CancellationToken cancellationToken, ITextSnapshot snapshot)
        {
            try
            {
                // Capture text and analyze on a background thread immediately (no debounce delay).
                await Task.Run(() =>
                {
                    string text = snapshot.GetText();
                    PerformAnalysis(buffer, snapshot, text, filePath, pendingAnalysis, cancellationToken);
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Analysis was cancelled
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource was disposed
            }
            finally
            {
                ClearPendingAnalysisIfMatches(buffer, pendingAnalysis);
            }
        }

        /// <summary>
        /// Performs the actual analysis and updates the cache.
        /// </summary>
        private void PerformAnalysis(
            ITextBuffer buffer,
            ITextSnapshot snapshot,
            string text,
            string filePath,
            PendingAnalysis pendingAnalysis,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Return empty violations if linting is disabled
                List<LintViolation> violations;
                if (GeneralOptions.Instance.LintingEnabled)
                {
                    IReadOnlyDictionary<string, object> codingConventions = GetEditorOptions(buffer)
                        .GetOptionValue(DefaultOptions.RawCodingConventionsSnapshotOptionId);
                    violations = [.. MarkdownLintAnalyzer.Instance.Analyze(
                        text,
                        filePath,
                        codingConventions,
                        cancellationToken)];
                }
                else
                {
                    violations = [];
                }

                if (cancellationToken.IsCancellationRequested ||
                    buffer.CurrentSnapshot.Version.VersionNumber != snapshot.Version.VersionNumber ||
                    !buffer.Properties.TryGetProperty(_pendingAnalysisKey, out PendingAnalysis currentPending) ||
                    !ReferenceEquals(currentPending, pendingAnalysis))
                {
                    return;
                }

                var result = new CachedAnalysisResult(snapshot.Version.VersionNumber, violations);
                buffer.Properties[_propertyKey] = result;

                AnalysisUpdated?.Invoke(this, new AnalysisUpdatedEventArgs(buffer, snapshot, violations, filePath));
            }
            catch (OperationCanceledException)
            {
                // Analysis was cancelled — don't update cache or notify listeners
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
            if (buffer.Properties.TryGetProperty(_pendingAnalysisKey, out PendingAnalysis pendingAnalysis))
            {
                _ = buffer.Properties.RemoveProperty(_pendingAnalysisKey);
                // Cancel first, then dispose - order matters for race condition safety
                // The token is passed by value to the async method, so accessing IsCancellationRequested
                // after Cancel() is safe, but we should not dispose until after Task.Delay returns
                try
                {
                    pendingAnalysis.CancellationTokenSource.Cancel();
                }
                finally
                {
                    // Dispose is safe here because Task.Delay will throw OperationCanceledException
                    // before accessing the CTS again, and we catch ObjectDisposedException as a fallback
                    pendingAnalysis.CancellationTokenSource.Dispose();
                }
            }
        }

        private static bool HasPendingAnalysisForSnapshot(ITextBuffer buffer, int snapshotVersion)
        {
            return buffer.Properties.TryGetProperty(_pendingAnalysisKey, out PendingAnalysis pendingAnalysis)
                && pendingAnalysis.SnapshotVersion == snapshotVersion;
        }

        private static bool HasPendingImmediateAnalysisForSnapshot(ITextBuffer buffer, int snapshotVersion)
        {
            return buffer.Properties.TryGetProperty(_pendingAnalysisKey, out PendingAnalysis pendingAnalysis)
                && pendingAnalysis.SnapshotVersion == snapshotVersion
                && !pendingAnalysis.IsDebounced;
        }

        private static void ClearPendingAnalysisIfMatches(ITextBuffer buffer, PendingAnalysis completedAnalysis)
        {
            if (buffer.Properties.TryGetProperty(_pendingAnalysisKey, out PendingAnalysis pendingAnalysis)
                // A newer pending analysis can target the same snapshot, so match the exact operation.
                && ReferenceEquals(pendingAnalysis, completedAnalysis))
            {
                _ = buffer.Properties.RemoveProperty(_pendingAnalysisKey);
                pendingAnalysis.CancellationTokenSource.Dispose();
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
