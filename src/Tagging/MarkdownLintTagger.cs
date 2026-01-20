using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
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
    [TagType(typeof(IErrorTag))]
    public class MarkdownLintTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            if (buffer == null)
                return null;

            return buffer.Properties.GetOrCreateSingletonProperty(
                typeof(MarkdownLintTagger),
                () => new MarkdownLintTagger(buffer)) as ITagger<T>;
        }
    }

    /// <summary>
    /// Tagger that provides error tags for markdown lint violations.
    /// </summary>
    public class MarkdownLintTagger : ITagger<IErrorTag>, IDisposable
    {
        private readonly ITextBuffer _buffer;
        private ITextSnapshot _currentSnapshot;
        private List<LintResult> _currentResults;
        private bool _isDisposed;
        private readonly object _lock = new();

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public MarkdownLintTagger(ITextBuffer buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _currentSnapshot = buffer.CurrentSnapshot;
            _currentResults = [];

            _buffer.Changed += OnBufferChanged;

            // Initial analysis
            Analyze();
        }

        private void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // Debounce - only analyze after typing stops
            _currentSnapshot = e.After;
            Analyze();
        }

        private void Analyze()
        {
            ITextSnapshot snapshot = _currentSnapshot;
            var text = snapshot.GetText();
            var filePath = GetFilePath();

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var violations = Linting.MarkdownLintAnalyzer.Instance.Analyze(text, filePath).ToList();
                    var results = violations.Select(v => new LintResult(v, snapshot)).ToList();

                    lock (_lock)
                    {
                        if (snapshot.Version.VersionNumber >= _currentSnapshot.Version.VersionNumber)
                        {
                            _currentResults = results;

                            // Raise tags changed on UI thread
                            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                                new SnapshotSpan(snapshot, 0, snapshot.Length)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log("Markdown lint analysis failed");
                }
            });
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
                        new ErrorTag(GetErrorType(result.Severity), result.Message));
                }
            }
        }

        private string GetErrorType(Linting.DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case Linting.DiagnosticSeverity.Error:
                    return Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.SyntaxError;
                case Linting.DiagnosticSeverity.Warning:
                    return Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.Warning;
                case Linting.DiagnosticSeverity.Suggestion:
                    return Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.Suggestion;
                default:
                    return Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.HintedSuggestion;
            }
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
