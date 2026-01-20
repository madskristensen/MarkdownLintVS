using System;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add a blank line before content (MD022, MD031, MD032, MD058).
    /// </summary>
    public class AddBlankLineBeforeAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add blank line before";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(line.Start, Environment.NewLine);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return Environment.NewLine + line.GetText();
        }
    }
}
