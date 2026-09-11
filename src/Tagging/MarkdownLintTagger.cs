using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.Tagging
{
    /// <summary>
    /// Provides the tagger for markdown files.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    [TagType(typeof(IErrorTag))]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class MarkdownLintTaggerProvider : IViewTaggerProvider
    {
        [Import]
        internal MarkdownAnalysisCache AnalysisCache { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView == null || buffer == null)
                return null;

            return buffer.Properties.GetOrCreateSingletonProperty(
                typeof(MarkdownLintTagger),
                () => new MarkdownLintTagger(buffer, AnalysisCache)) as ITagger<T>;
        }
    }

    /// <summary>
    /// Tagger that provides error tags for markdown lint violations.
    /// Uses shared MarkdownAnalysisCache to avoid duplicate parsing.
    /// </summary>
    public class MarkdownLintTagger : ITagger<IErrorTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly MarkdownAnalysisCache _analysisCache;
        private readonly IEditorOptions _editorOptions;
        private readonly ITextDocument _document;
        private readonly DocumentPathTracker _pathTracker;
        private ITextSnapshot _currentSnapshot;
        private List<LintResult> _currentResults;
        private bool _isDisposed;
        private readonly object _lock = new();

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public MarkdownLintTagger(ITextBuffer buffer, MarkdownAnalysisCache analysisCache)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _analysisCache = analysisCache ?? throw new ArgumentNullException(nameof(analysisCache));
            _currentSnapshot = buffer.CurrentSnapshot;
            _currentResults = [];
            _buffer.Properties.TryGetProperty(typeof(ITextDocument), out _document);
            _pathTracker = new DocumentPathTracker(_document?.FilePath);
            _editorOptions = _analysisCache.GetEditorOptions(_buffer);

            _buffer.Changed += OnBufferChanged;
            if (_document != null)
                _document.FileActionOccurred += OnFileActionOccurred;
            _editorOptions.OptionChanged += OnEditorOptionChanged;
            RuleOptions.Saved += OnRuleOptionsSaved;
            GeneralOptions.Saved += OnGeneralOptionsSaved;
            _analysisCache.AnalysisUpdated += OnAnalysisUpdated;

            // Initial analysis - immediate, no debounce for fast feedback on file open
            _analysisCache.AnalyzeImmediate(_buffer, _pathTracker.CurrentPath);
        }

        private void OnEditorOptionChanged(object sender, EditorOptionChangedEventArgs e)
        {
            if (e.OptionId == DefaultOptions.RawCodingConventionsSnapshotOptionName)
                Reanalyze();
        }

        internal void Reanalyze()
        {
            _analysisCache.Reanalyze(_buffer, _pathTracker.CurrentPath);
        }

        private void OnRuleOptionsSaved(RuleOptions options)
        {
            // Revalidate immediately when options change - no debounce needed
            _analysisCache.AnalyzeImmediate(_buffer, _pathTracker.CurrentPath);
        }

        private void OnGeneralOptionsSaved(GeneralOptions options)
        {
            if (!options.LintingEnabled)
            {
                ClearCurrentResults();
            }

            // Revalidate immediately when linting is enabled/disabled
            _analysisCache.AnalyzeImmediate(_buffer, _pathTracker.CurrentPath);
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            lock (_lock)
            {
                _currentSnapshot = e.After;
            }

            // Keep existing results while debounced analysis runs. Tracking spans translate stale
            // squiggles to the edited snapshot, and the next analysis replaces or clears them.
            _analysisCache.InvalidateAndAnalyze(_buffer, _pathTracker.CurrentPath);
        }

        private void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.DocumentRenamed &&
                _pathTracker.Update(e.FilePath) != null)
            {
                Reanalyze();
            }
        }

        private void OnAnalysisUpdated(object sender, AnalysisUpdatedEventArgs e)
        {
            if (e.Buffer != _buffer)
                return;

            ITextSnapshot snapshot = e.Snapshot;
            var results = e.Violations
                .Select(v => new LintResult(v, snapshot))
                .OrderBy(r => r.Start)
                .ToList();
            SnapshotSpan? affectedSpan = null;

            lock (_lock)
            {
                if (snapshot.Version.VersionNumber >= _currentSnapshot.Version.VersionNumber)
                {
                    affectedSpan = GetAffectedSpan(snapshot, _currentResults, results);
                    _currentResults = results;
                }
            }

            if (affectedSpan.HasValue)
            {
                RaiseTagsChanged(affectedSpan);
            }
        }

        private void ClearCurrentResults()
        {
            var shouldRaiseTagsChanged = false;

            lock (_lock)
            {
                if (_currentResults.Count > 0)
                {
                    _currentResults = [];
                    shouldRaiseTagsChanged = true;
                }
            }

            if (shouldRaiseTagsChanged)
            {
                RaiseTagsChanged();
            }
        }

        private void RaiseTagsChanged(SnapshotSpan? affectedSpan = null)
        {
            if (ThreadHelper.CheckAccess())
            {
                RaiseTagsChangedOnMainThread(affectedSpan);
                return;
            }
#pragma warning disable VSSDK007 // ThreadHelper.JoinableTaskFactory.RunAsync fire-and-forget is intentional for event-driven refresh
            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                RaiseTagsChangedOnMainThread(affectedSpan);
            }).FireAndForget();
        }
#pragma warning restore VSSDK007

        private void RaiseTagsChangedOnMainThread(SnapshotSpan? affectedSpan)
        {
            if (_isDisposed)
            {
                return;
            }

            ITextSnapshot snapshot = _buffer.CurrentSnapshot;
            SnapshotSpan span = affectedSpan.HasValue && affectedSpan.Value.Snapshot == snapshot
                ? affectedSpan.Value
                : new SnapshotSpan(snapshot, 0, snapshot.Length);
            EventHandler<SnapshotSpanEventArgs> tagsChanged = TagsChanged;
            tagsChanged?.Invoke(this, new SnapshotSpanEventArgs(span));
        }

        private static SnapshotSpan? GetAffectedSpan(
            ITextSnapshot snapshot,
            IReadOnlyList<LintResult> oldResults,
            IReadOnlyList<LintResult> newResults)
        {
            var start = int.MaxValue;
            var end = 0;
            var hasResults = false;

            foreach (LintResult result in oldResults.Concat(newResults))
            {
                SnapshotSpan? span = result.GetTranslatedSpan(snapshot);
                if (!span.HasValue)
                {
                    return new SnapshotSpan(snapshot, 0, snapshot.Length);
                }

                hasResults = true;
                start = Math.Min(start, span.Value.Start.Position);
                end = Math.Max(end, span.Value.End.Position);
            }

            return hasResults
                ? new SnapshotSpan(snapshot, start, Math.Max(0, end - start))
                : null;
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            List<LintResult> results;
            lock (_lock)
            {
                results = _currentResults;
            }

            ITextSnapshot currentSnapshot = spans[0].Snapshot;
            var queryStart = spans[0].Start.Position;
            var queryEnd = spans[spans.Count - 1].End.Position;

            foreach (LintResult result in results)
            {
                if (result.Start > queryEnd)
                {
                    break;
                }

                SnapshotSpan? span = result.GetTranslatedSpan(currentSnapshot);
                if (!span.HasValue)
                {
                    continue;
                }

                if (span.Value.Length == 0)
                {
                    continue;
                }

                if (span.Value.End.Position < queryStart)
                {
                    continue;
                }

                if (IntersectsAnySpan(span.Value, spans))
                {
                    yield return new TagSpan<IErrorTag>(
                        span.Value,
                        new ErrorTag(GetErrorType(result.Severity)));
                }
            }
        }

        private static bool IntersectsAnySpan(SnapshotSpan target, NormalizedSnapshotSpanCollection spans)
        {
            for (var i = 0; i < spans.Count; i++)
            {
                SnapshotSpan candidate = spans[i];

                if (candidate.End.Position < target.Start.Position)
                    continue;

                if (candidate.Start.Position > target.End.Position)
                    return false;

                if (candidate.IntersectsWith(target))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all lint results that contain the specified point.
        /// Used by QuickInfo to display error details.
        /// </summary>
        public IEnumerable<LintResult> GetLintResultsAtPoint(SnapshotPoint point)
        {
            List<LintResult> results;
            lock (_lock)
            {
                results = _currentResults;
            }

            int index = UpperBoundByStart(results, point.Position) - 1;
            for (; index >= 0; index--)
            {
                LintResult result = results[index];
                SnapshotSpan? span = result.GetTranslatedSpan(point.Snapshot);
                if (span.HasValue && span.Value.Length > 0 && span.Value.Contains(point))
                {
                    yield return result;
                }
            }
        }

        private static int UpperBoundByStart(IReadOnlyList<LintResult> results, int position)
        {
            var low = 0;
            var high = results.Count;
            while (low < high)
            {
                int middle = low + ((high - low) / 2);
                if (results[middle].Start <= position)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private string GetErrorType(Linting.DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.SyntaxError,
                DiagnosticSeverity.Warning => Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.Warning,
                DiagnosticSeverity.Suggestion => Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.Suggestion,
                _ => Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.HintedSuggestion,
            };
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _buffer.Changed -= OnBufferChanged;
                if (_document != null)
                    _document.FileActionOccurred -= OnFileActionOccurred;
                _editorOptions.OptionChanged -= OnEditorOptionChanged;
                RuleOptions.Saved -= OnRuleOptionsSaved;
                GeneralOptions.Saved -= OnGeneralOptionsSaved;
                _analysisCache.AnalysisUpdated -= OnAnalysisUpdated;
                _isDisposed = true;
            }
        }
    }

    internal sealed class DocumentPathTracker(string initialPath)
    {
        public string CurrentPath { get; private set; } = initialPath;

        public string Update(string newPath)
        {
            if (string.Equals(CurrentPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string oldPath = CurrentPath;
            CurrentPath = newPath;
            return oldPath;
        }
    }

    /// <summary>
    /// Represents a lint result with tracking span support.
    /// </summary>
    public class LintResult
    {
        private readonly ITrackingSpan _trackingSpan;

        public string RuleId { get; }
        public string Message { get; }
        public string DocumentationUrl { get; }
        public Linting.DiagnosticSeverity Severity { get; }
        public int Start { get; }

        public LintResult(Linting.LintViolation violation, ITextSnapshot snapshot)
        {
            RuleId = violation.Rule.Id;
            Message = $"{violation.Rule.Id}: {violation.Message}";
            DocumentationUrl = violation.Rule.DocumentationUrl;
            Severity = violation.Severity;

            // Calculate span from line/column
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(Math.Min(violation.LineNumber, snapshot.LineCount - 1));
            var startIndex = line.Start.Position + Math.Min(violation.ColumnStart, line.Length);
            var endIndex = line.Start.Position + Math.Min(violation.ColumnEnd, line.Length);

            if (endIndex <= startIndex)
                endIndex = Math.Min(startIndex + 1, line.End.Position);

            // Clamp to snapshot length to avoid ArgumentOutOfRangeException
            startIndex = Math.Min(startIndex, snapshot.Length);
            endIndex = Math.Min(endIndex, snapshot.Length);

            var length = Math.Max(0, endIndex - startIndex);
            var span = new Span(startIndex, length);
            Start = span.Start;
            _trackingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
        }

        public SnapshotSpan? GetTranslatedSpan(ITextSnapshot snapshot)
        {
            try
            {
                return _trackingSpan.GetSpan(snapshot);
            }
            catch
            {
                return null;
            }
        }
    }
}
