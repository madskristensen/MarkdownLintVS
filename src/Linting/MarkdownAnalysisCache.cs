using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Cached analysis result for a text buffer.
    /// </summary>
    internal class CachedAnalysisResult
    {
        public int SnapshotVersion { get; }
        public IReadOnlyList<LintViolation> Violations { get; }

        public CachedAnalysisResult(int snapshotVersion, IReadOnlyList<LintViolation> violations)
        {
            SnapshotVersion = snapshotVersion;
            Violations = violations;
        }
    }

    /// <summary>
    /// Provides shared analysis caching for markdown documents.
    /// Both the tagger and error list use this to avoid duplicate parsing.
    /// </summary>
    [Export(typeof(MarkdownAnalysisCache))]
    public class MarkdownAnalysisCache
    {
        private static readonly object _propertyKey = typeof(MarkdownAnalysisCache);

        /// <summary>
        /// Event raised when analysis results are updated for a buffer.
        /// </summary>
        public event EventHandler<AnalysisUpdatedEventArgs> AnalysisUpdated;

        /// <summary>
        /// Gets cached violations for a buffer, or analyzes if cache is stale.
        /// </summary>
        public IReadOnlyList<LintViolation> GetOrAnalyze(ITextBuffer buffer, string filePath)
        {
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
        /// Triggers analysis and notifies all listeners.
        /// Call this when the buffer changes or options change.
        /// </summary>
        public void InvalidateAndAnalyze(ITextBuffer buffer, string filePath)
        {
            ITextSnapshot snapshot = buffer.CurrentSnapshot;
            var text = snapshot.GetText();

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var violations = MarkdownLintAnalyzer.Instance.Analyze(text, filePath).ToList();
                    var result = new CachedAnalysisResult(snapshot.Version.VersionNumber, violations);

                    buffer.Properties[_propertyKey] = result;

                    AnalysisUpdated?.Invoke(this, new AnalysisUpdatedEventArgs(buffer, snapshot, violations, filePath));
                }
                catch (Exception ex)
                {
                    ex.Log("Shared markdown analysis failed");
                }
            });
        }

        /// <summary>
        /// Clears the cache for a buffer.
        /// </summary>
        public void Invalidate(ITextBuffer buffer)
        {
            buffer.Properties.RemoveProperty(_propertyKey);
        }
    }

    /// <summary>
    /// Event args for analysis completion.
    /// </summary>
    public class AnalysisUpdatedEventArgs : EventArgs
    {
        public ITextBuffer Buffer { get; }
        public ITextSnapshot Snapshot { get; }
        public IReadOnlyList<LintViolation> Violations { get; }
        public string FilePath { get; }

        public AnalysisUpdatedEventArgs(
            ITextBuffer buffer,
            ITextSnapshot snapshot,
            IReadOnlyList<LintViolation> violations,
            string filePath)
        {
            Buffer = buffer;
            Snapshot = snapshot;
            Violations = violations;
            FilePath = filePath;
        }
    }
}
