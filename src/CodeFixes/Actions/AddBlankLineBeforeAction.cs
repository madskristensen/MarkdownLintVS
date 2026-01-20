using System;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add a blank line before content (MD022, MD031, MD032, MD058).
    /// </summary>
    public class AddBlankLineBeforeAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add blank line before";

        /// <summary>
        /// Gets the position where the blank line will be inserted.
        /// Used for deduplication in Fix All operations.
        /// </summary>
        public int InsertPosition
        {
            get
            {
                ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
                return line.Start.Position;
            }
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Insert(line.Start, Environment.NewLine);
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return Environment.NewLine + line.GetText();
        }
    }
}
