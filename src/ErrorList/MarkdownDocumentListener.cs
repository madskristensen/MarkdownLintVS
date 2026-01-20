using System.ComponentModel.Composition;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.ErrorList
{
    /// <summary>
    /// Listens for document changes and updates the error list.
    /// Uses shared MarkdownAnalysisCache to avoid duplicate parsing.
    /// </summary>
    [Export(typeof(ITextViewCreationListener))]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class MarkdownDocumentListener : ITextViewCreationListener
    {
        [Import]
        internal MarkdownLintTableDataSource TableDataSource { get; set; }

        [Import]
        internal MarkdownAnalysisCache AnalysisCache { get; set; }

        public void TextViewCreated(ITextView textView)
        {
            var filePath = GetFilePath(textView);
            var handler = new DocumentHandler(textView, TableDataSource, AnalysisCache, filePath);
            textView.Closed += (s, e) => handler.Dispose();
        }

        private string GetFilePath(ITextView textView)
        {
            if (textView.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document))
            {
                return document.FilePath;
            }
            return null;
        }
    }

    /// <summary>
    /// Handles document events for a specific text view.
    /// Listens to shared analysis cache for results.
    /// </summary>
    internal class DocumentHandler : IDisposable
    {
        private readonly ITextView _textView;
        private readonly MarkdownLintTableDataSource _tableDataSource;
        private readonly MarkdownAnalysisCache _analysisCache;
        private readonly string _filePath;
        private readonly System.Timers.Timer _debounceTimer;
        private bool _disposed;

        public DocumentHandler(
            ITextView textView,
            MarkdownLintTableDataSource tableDataSource,
            MarkdownAnalysisCache analysisCache,
            string filePath)
        {
            _textView = textView;
            _tableDataSource = tableDataSource;
            _analysisCache = analysisCache;
            _filePath = filePath;

            _debounceTimer = new System.Timers.Timer(500)
            {
                AutoReset = false
            };
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;

            _textView.TextBuffer.Changed += OnTextBufferChanged;
            _analysisCache.AnalysisUpdated += OnAnalysisUpdated;

            // Initial analysis - request from cache
            RequestAnalysis();
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // Reset debounce timer
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnDebounceTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            RequestAnalysis();
        }

        private void OnAnalysisUpdated(object sender, AnalysisUpdatedEventArgs e)
        {
            if (e.Buffer != _textView.TextBuffer)
                return;

            // Update error list with new results
            _tableDataSource?.UpdateErrors(_filePath, e.Violations);
        }

        private void RequestAnalysis()
        {
            if (_disposed)
                return;

            // The cache will analyze and notify via AnalysisUpdated event
            _analysisCache.InvalidateAndAnalyze(_textView.TextBuffer, _filePath);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
                _textView.TextBuffer.Changed -= OnTextBufferChanged;
                _analysisCache.AnalysisUpdated -= OnAnalysisUpdated;
                _tableDataSource?.ClearErrors(_filePath);
            }
        }
    }
}
