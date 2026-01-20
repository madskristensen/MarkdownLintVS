using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Options;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.Tagging
{
    /// <summary>
    /// Provides the tagger for markdown files.
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    [TagType(typeof(IErrorTag))]
    public class MarkdownLintTaggerProvider : ITaggerProvider
    {
        [Import]
        internal MarkdownAnalysisCache AnalysisCache { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
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
            RuleOptions.Saved += OnOptionsSaved;
            _analysisCache.AnalysisUpdated += OnAnalysisUpdated;

            // Initial analysis
            RequestAnalysis();
        }

        private void OnOptionsSaved(RuleOptions options)
        {
            // Revalidate when options change
            RequestAnalysis();
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            _currentSnapshot = e.After;
            RequestAnalysis();
        }

        private void OnAnalysisUpdated(object sender, AnalysisUpdatedEventArgs e)
        {
            if (e.Buffer != _buffer)
                return;

            ITextSnapshot snapshot = e.Snapshot;
            var results = e.Violations.Select(v => new LintResult(v, snapshot)).ToList();

            lock (_lock)
            {
                if (snapshot.Version.VersionNumber >= _currentSnapshot.Version.VersionNumber)
                {
                    _currentResults = results;

                    TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                        new SnapshotSpan(snapshot, 0, snapshot.Length)));
                }
            }
        }

        private void RequestAnalysis()
        {
            _analysisCache.InvalidateAndAnalyze(_buffer, _filePath);
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

            foreach (LintResult result in results)
            {
                SnapshotSpan? span = result.GetTranslatedSpan(currentSnapshot);
                if (span.HasValue && spans.Any(s => s.IntersectsWith(span.Value)))
                {
                    yield return new TagSpan<IErrorTag>(
                        span.Value,
                        new ErrorTag(GetErrorType(result.Severity)));
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
                RuleOptions.Saved -= OnOptionsSaved;
                _analysisCache.AnalysisUpdated -= OnAnalysisUpdated;
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// Represents a lint result with tracking span support.
    /// </summary>
    internal class LintResult
    {
        private readonly ITrackingSpan _trackingSpan;

        public string RuleId { get; }
        public string Message { get; }
        public Linting.DiagnosticSeverity Severity { get; }

        public LintResult(Linting.LintViolation violation, ITextSnapshot snapshot)
        {
            RuleId = violation.Rule.Id;
            Message = $"{violation.Rule.Id}: {violation.Message}";
            Severity = violation.Severity;

            // Calculate span from line/column
            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(Math.Min(violation.LineNumber, snapshot.LineCount - 1));
            var startIndex = line.Start.Position + Math.Min(violation.ColumnStart, line.Length);
            var endIndex = line.Start.Position + Math.Min(violation.ColumnEnd, line.Length);

            if (endIndex <= startIndex)
                endIndex = Math.Min(startIndex + 1, line.End.Position);

            var span = new Span(startIndex, Math.Max(1, endIndex - startIndex));
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
