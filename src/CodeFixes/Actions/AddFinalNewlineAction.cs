using System;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add a newline at end of file (MD047).
    /// </summary>
    public class AddFinalNewlineAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add newline at end of file";

        public override void Invoke(CancellationToken cancellationToken)
        {
            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(Snapshot.Length, Environment.NewLine);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return Environment.NewLine;
        }
    }
}
