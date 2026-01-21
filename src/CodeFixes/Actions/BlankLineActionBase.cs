using System;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Specifies where to insert a blank line relative to content.
    /// </summary>
    public enum BlankLinePosition
    {
        /// <summary>Insert blank line before the content.</summary>
        Before,
        /// <summary>Insert blank line after the content.</summary>
        After
    }

    /// <summary>
    /// Base class for fix actions that add blank lines before or after content.
    /// Used for MD022, MD031, MD032, MD058.
    /// </summary>
    public abstract class BlankLineActionBase(ITextSnapshot snapshot, Span span, BlankLinePosition position) 
        : MarkdownFixAction(snapshot, span)
    {
        protected BlankLinePosition Position { get; } = position;

        public override string DisplayText => Position == BlankLinePosition.Before 
            ? "Add blank line before" 
            : "Add blank line after";

        /// <summary>
        /// Gets the position where the blank line will be inserted.
        /// Used for deduplication in Fix All operations.
        /// </summary>
        public int InsertPosition
        {
            get
            {
                ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
                return Position == BlankLinePosition.Before 
                    ? line.Start.Position 
                    : line.EndIncludingLineBreak.Position;
            }
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            
            if (Position == BlankLinePosition.Before)
                edit.Insert(line.Start, Environment.NewLine);
            else
                edit.Insert(line.EndIncludingLineBreak, Environment.NewLine);
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            
            return Position == BlankLinePosition.Before
                ? Environment.NewLine + line.GetText()
                : line.GetText() + Environment.NewLine;
        }
    }
}
