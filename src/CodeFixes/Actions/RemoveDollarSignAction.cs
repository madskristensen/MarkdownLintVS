using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove dollar sign from command (MD014).
    /// </summary>
    [FixForRule("MD014")]
    public class RemoveDollarSignAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove $ from command";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return Regex.Replace(line.GetText(), @"^\$\s?", "");
        }
    }
}
