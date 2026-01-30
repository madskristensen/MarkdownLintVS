using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownLintVS.Completion
{
    /// <summary>
    /// Handles keyboard commands to trigger completion in markdownlint suppression comments.
    /// </summary>
    [Export(typeof(IVsTextViewCreationListener))]
    [Name("Markdown Lint Suppression Completion Handler")]
    [ContentType("markdown")]
    [ContentType("vs-markdown")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class SuppressionCommentCompletionHandlerProvider : IVsTextViewCreationListener
    {
        [Import]
        internal IVsEditorAdaptersFactoryService AdapterService { get; set; }

        [Import]
        internal ICompletionBroker CompletionBroker { get; set; }

        [Import]
        internal SVsServiceProvider ServiceProvider { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            ITextView textView = AdapterService.GetWpfTextView(textViewAdapter);
            if (textView == null)
                return;

            textView.Properties.GetOrCreateSingletonProperty(
                typeof(SuppressionCommentCompletionCommandHandler),
                () => new SuppressionCommentCompletionCommandHandler(textViewAdapter, textView, CompletionBroker));
        }
    }

    /// <summary>
    /// Command handler that triggers completion when typing in suppression comments.
    /// </summary>
    internal sealed class SuppressionCommentCompletionCommandHandler : IOleCommandTarget
    {
        // Pattern to detect if we're inside a markdownlint suppression comment
        private static readonly Regex _suppressionCommentPattern = new(
            @"<!--\s*markdownlint-(disable|enable|disable-line|disable-next-line|disable-file)\s",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IOleCommandTarget _nextCommandHandler;
        private readonly ITextView _textView;
        private readonly ICompletionBroker _broker;
        private ICompletionSession _session;

        public SuppressionCommentCompletionCommandHandler(
            IVsTextView textViewAdapter,
            ITextView textView,
            ICompletionBroker broker)
        {
            _textView = textView;
            _broker = broker;

            // Add the command handler to the chain
            textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (VsShellUtilities.IsInAutomationFunction(ServiceProvider.GlobalProvider))
            {
                return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            // Check for commit characters
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN)
            {
                // Commit on Enter
                if (_session != null && !_session.IsDismissed)
                {
                    if (_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        _session.Commit();
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        _session.Dismiss();
                    }
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
            {
                // Commit on Tab
                if (_session != null && !_session.IsDismissed)
                {
                    if (_session.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        _session.Commit();
                        return VSConstants.S_OK;
                    }
                    else
                    {
                        _session.Dismiss();
                    }
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
            {
                // Dismiss on Escape
                if (_session != null && !_session.IsDismissed)
                {
                    _session.Dismiss();
                    return VSConstants.S_OK;
                }
            }

            // Pass along the command first
            var retVal = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            // Handle character typing
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                var typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

                if (_session == null || _session.IsDismissed)
                {
                    // Check if we should trigger completion
                    if (ShouldTriggerCompletion(typedChar))
                    {
                        TriggerCompletion();
                    }
                }
                else
                {
                    // Filter the completion list
                    _session.Filter();
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE)
            {
                // Handle backspace - filter or dismiss
                if (_session != null && !_session.IsDismissed)
                {
                    _session.Filter();
                }
            }

            return retVal;
        }

        private bool ShouldTriggerCompletion(char typedChar)
        {
            // Trigger on space after directive, or on alphanumeric for typing rule names
            if (!char.IsLetterOrDigit(typedChar) && typedChar != ' ' && typedChar != '-')
                return false;

            // Check if we're in a suppression comment
            SnapshotPoint? caretPoint = _textView.Caret.Position.Point.GetPoint(
                textBuffer => !textBuffer.ContentType.IsOfType("projection"),
                PositionAffinity.Predecessor);

            if (!caretPoint.HasValue)
                return false;

            ITextSnapshotLine line = caretPoint.Value.GetContainingLine();
            var lineText = line.GetText();
            var cursorPos = caretPoint.Value.Position - line.Start.Position;

            if (cursorPos <= 0)
                return false;

            var textBeforeCursor = lineText.Substring(0, cursorPos);

            // Check if we're inside a markdownlint suppression comment
            if (!_suppressionCommentPattern.IsMatch(textBeforeCursor))
                return false;

            // Make sure we haven't closed the comment
            if (textBeforeCursor.Contains("-->"))
                return false;

            return true;
        }

        private void TriggerCompletion()
        {
            // Get the caret position
            SnapshotPoint? caretPoint = _textView.Caret.Position.Point.GetPoint(
                textBuffer => !textBuffer.ContentType.IsOfType("projection"),
                PositionAffinity.Predecessor);

            if (!caretPoint.HasValue)
                return;

            // Trigger completion
            _session = _broker.TriggerCompletion(_textView);

            if (_session != null)
            {
                _session.Dismissed += OnSessionDismissed;
            }
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
            if (_session != null)
            {
                _session.Dismissed -= OnSessionDismissed;
                _session = null;
            }
        }
    }
}
