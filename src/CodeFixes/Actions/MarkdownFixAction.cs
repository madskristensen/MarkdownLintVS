using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Base class for markdown lint fix actions.
    /// </summary>
    public abstract class MarkdownFixAction(ITextSnapshot snapshot, Span span) : ISuggestedAction
    {
        protected readonly ITextSnapshot Snapshot = snapshot;
        protected readonly Span Span = span;

        public abstract string DisplayText { get; }
        public virtual string IconAutomationText => null;
        public virtual ImageMoniker IconMoniker => default;
        public virtual string InputGestureText => null;
        public virtual bool HasActionSets => false;
        public virtual bool HasPreview => true;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            ITrackingSpan trackingSpan = Snapshot.CreateTrackingSpan(Span, SpanTrackingMode.EdgeExclusive);
            var previewText = GetFixedText();

            return Task.FromResult<object>(previewText);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            using ITextEdit edit = Snapshot.TextBuffer.CreateEdit();
            ApplyFix(edit);
            edit.Apply();
        }

        /// <summary>
        /// Applies the fix to the given edit. Used by both Invoke and FixAll operations.
        /// </summary>
        public abstract void ApplyFix(ITextEdit edit);

        protected abstract string GetFixedText();

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        void IDisposable.Dispose() { }
    }
}
