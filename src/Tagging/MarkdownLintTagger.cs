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
        private readonly string _filePath;
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
            _filePath = GetFilePath();

            _buffer.Changed += OnBufferChanged;
            RuleOptions.Saved += OnRuleOptionsSaved;
            GeneralOptions.Saved += OnGeneralOptionsSaved;
            _analysisCache.AnalysisUpdated += OnAnalysisUpdated;

            // Initial analysis - immediate, no debounce for fast feedback on file open
            _analysisCache.AnalyzeImmediate(_buffer, _filePath);
        }

        private void OnRuleOptionsSaved(RuleOptions options)
        {
            // Revalidate immediately when options change - no debounce needed
            _analysisCache.AnalyzeImmediate(_buffer, _filePath);
        }

        private void OnGeneralOptionsSaved(GeneralOptions options)
        {
            // Revalidate immediately when linting is enabled/disabled
            _analysisCache.AnalyzeImmediate(_buffer, _filePath);
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _currentSnapshot = e.After;

            ClearCurrentResults(e.After);

            // Debounced analysis during typing to reduce CPU usage
            _analysisCache.InvalidateAndAnalyze(_buffer, _filePath);
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
            var shouldRaiseTagsChanged = false;

            lock (_lock)
            {
                if (snapshot.Version.VersionNumber >= _currentSnapshot.Version.VersionNumber)
                {
                    _currentResults = results;
                    shouldRaiseTagsChanged = true;
                }
            }

            if (shouldRaiseTagsChanged)
            {
                RaiseTagsChanged(snapshot);
            }
        }

        private void ClearCurrentResults(ITextSnapshot snapshot)
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
                RaiseTagsChanged(snapshot);
            }
        }

        private void RaiseTagsChanged(ITextSnapshot snapshot)
        {
            if (ThreadHelper.CheckAccess())
            {
                RaiseTagsChangedOnMainThread(snapshot);
                return;
            }

            ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                RaiseTagsChangedOnMainThread(snapshot);
            }).FireAndForget();
        }

        private void RaiseTagsChangedOnMainThread(ITextSnapshot snapshot)
        {
            if (_isDisposed)
            {
                return;
            }

            EventHandler<SnapshotSpanEventArgs> tagsChanged = TagsChanged;
            tagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            List<LintResult> results;
            lock (_lock)
            {
                results = [.. _currentResults];
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
                results = [.. _currentResults];
            }

            foreach (LintResult result in results)
            {
                SnapshotSpan? span = result.GetTranslatedSpan(point.Snapshot);
                if (span.HasValue && span.Value.Contains(point))
                {
                    yield return result;
                }
            }
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

        private string GetFilePath()
        {
            if (_buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
            {
                return document.FilePath;
            }
            return null;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _buffer.Changed -= OnBufferChanged;
                RuleOptions.Saved -= OnRuleOptionsSaved;
                GeneralOptions.Saved -= OnGeneralOptionsSaved;
                _analysisCache.AnalysisUpdated -= OnAnalysisUpdated;
                _isDisposed = true;
            }
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
