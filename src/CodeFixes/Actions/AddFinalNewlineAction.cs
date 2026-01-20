using System;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add a newline at end of file (MD047).
    /// </summary>
    public class AddFinalNewlineAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add newline at end of file";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Insert(Snapshot.Length, Environment.NewLine);
        }

        protected override string GetFixedText()
        {
            return Environment.NewLine;
        }
    }
}
