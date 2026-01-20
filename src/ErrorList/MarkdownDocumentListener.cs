using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.ErrorList
{
    /// <summary>
    /// Listens for document changes and updates the error list.
    /// </summary>
    [Export(typeof(ITextViewCreationListener))]
    [ContentType("markdown")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class MarkdownDocumentListener : ITextViewCreationListener
    {
        [Import]
        internal MarkdownLintTableDataSource TableDataSource { get; set; }

        public void TextViewCreated(ITextView textView)
        {
            var filePath = GetFilePath(textView);
            var handler = new DocumentHandler(textView, TableDataSource, filePath);
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
    /// </summary>
    internal class DocumentHandler : IDisposable
    {
        private readonly ITextView _textView;
        private readonly MarkdownLintTableDataSource _tableDataSource;
        private readonly string _filePath;
        private readonly System.Timers.Timer _debounceTimer;
        private bool _disposed;

        public DocumentHandler(ITextView textView, MarkdownLintTableDataSource tableDataSource, string filePath)
        {
            _textView = textView;
            _tableDataSource = tableDataSource;
            _filePath = filePath;

            _debounceTimer = new System.Timers.Timer(500)
            {
                AutoReset = false
            };
            _debounceTimer.Elapsed += OnDebounceTimerElapsed;

            _textView.TextBuffer.Changed += OnTextBufferChanged;

            // Initial analysis
            AnalyzeDocument();
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // Reset debounce timer
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnDebounceTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            AnalyzeDocument();
        }

        private void AnalyzeDocument()
        {
            if (_disposed)
                return;

            try
            {
                var text = _textView.TextBuffer.CurrentSnapshot.GetText();
                var violations = Linting.MarkdownLintAnalyzer.Instance.Analyze(text, _filePath).ToList();

                _tableDataSource?.UpdateErrors(_filePath, violations);
            }
            catch (Exception ex)
            {
                ex.Log("Error List analysis failed");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _debounceTimer.Stop();
                _debounceTimer.Dispose();
                _textView.TextBuffer.Changed -= OnTextBufferChanged;
                _tableDataSource?.ClearErrors(_filePath);
            }
        }
    }
}
