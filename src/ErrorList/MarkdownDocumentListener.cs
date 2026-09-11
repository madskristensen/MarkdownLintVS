using System.ComponentModel.Composition;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
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
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class MarkdownDocumentListener : ITextViewCreationListener
    {
        [Import]
        internal MarkdownLintTableDataSource TableDataSource { get; set; }

        [Import]
        internal MarkdownAnalysisCache AnalysisCache { get; set; }

        public void TextViewCreated(ITextView textView)
        {
            if (textView.Roles.Contains(DifferenceViewerRoles.DiffTextViewRole))
                return;

            ITextBuffer buffer = textView.TextBuffer;
            DocumentHandler handler = buffer.Properties.GetOrCreateSingletonProperty(
                typeof(DocumentHandler),
                () => new DocumentHandler(buffer, TableDataSource, AnalysisCache, GetFilePath(textView)));
            handler.AddView();
            textView.Closed += OnTextViewClosed;

            void OnTextViewClosed(object sender, EventArgs e)
            {
                textView.Closed -= OnTextViewClosed;
                handler.RemoveView();
            }
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
    /// Note: Debouncing is handled by MarkdownAnalysisCache, not here.
    /// </summary>
    internal class DocumentHandler : IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly MarkdownLintTableDataSource _tableDataSource;
        private readonly MarkdownAnalysisCache _analysisCache;
        private readonly ITextDocument _document;
        private readonly DocumentPathTracker _pathTracker;
        private readonly ViewReferenceCounter _viewReferences = new();
        private bool _disposed;

        public DocumentHandler(
            ITextBuffer buffer,
            MarkdownLintTableDataSource tableDataSource,
            MarkdownAnalysisCache analysisCache,
            string filePath)
        {
            _buffer = buffer;
            _tableDataSource = tableDataSource;
            _analysisCache = analysisCache;
            _pathTracker = new DocumentPathTracker(filePath);
            _buffer.Properties.TryGetProperty(typeof(ITextDocument), out _document);

            // Only listen for analysis results — the tagger owns triggering analysis
            // (on buffer changes, option saves, and initial file open).
            _analysisCache.AnalysisUpdated += OnAnalysisUpdated;
            if (_document != null)
                _document.FileActionOccurred += OnFileActionOccurred;
        }

        public void AddView() => _viewReferences.Add();

        public void RemoveView()
        {
            if (_viewReferences.Remove())
            {
                Dispose();
            }
        }

        private void OnAnalysisUpdated(object sender, AnalysisUpdatedEventArgs e)
        {
            if (e.Buffer != _buffer)
                return;

            // Update error list with new results
            _tableDataSource?.UpdateErrors(_pathTracker.CurrentPath, e.Violations);

            // Register successful usage for rating prompt
            MarkdownLintVSPackage.RatingPrompt?.RegisterSuccessfulUsage();
        }

        private void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType != FileActionTypes.DocumentRenamed)
                return;

            string oldPath = _pathTracker.Update(e.FilePath);
            if (oldPath != null)
            {
                _tableDataSource?.ClearErrors(oldPath);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _analysisCache.AnalysisUpdated -= OnAnalysisUpdated;
                if (_document != null)
                    _document.FileActionOccurred -= OnFileActionOccurred;
                _tableDataSource?.ClearErrors(_pathTracker.CurrentPath);
                _ = _buffer.Properties.RemoveProperty(typeof(DocumentHandler));
            }
        }
    }

    internal sealed class ViewReferenceCounter
    {
        private int _count;

        internal int Count => _count;

        internal void Add() => _count++;

        internal bool Remove()
        {
            if (_count == 0)
            {
                return false;
            }

            _count--;
            return _count == 0;
        }
    }
}
