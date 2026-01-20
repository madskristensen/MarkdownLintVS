using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to remove dollar sign from command (MD014).
    /// </summary>
    public class RemoveDollarSignAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove $ from command";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var fixedText = RemoveDollarSign(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return RemoveDollarSign(line.GetText());
        }

        private static string RemoveDollarSign(string text)
        {
            // Remove leading $ and optional space from command line
            return Regex.Replace(text, @"^\$\s?", "");
        }
    }
}
