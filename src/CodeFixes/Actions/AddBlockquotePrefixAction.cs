using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add blockquote prefix to blank line inside blockquote (MD028).
    /// </summary>
    public class AddBlockquotePrefixAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add '>' prefix to blank line";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var fixedText = AddBlockquotePrefix(line.GetText());

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return AddBlockquotePrefix(line.GetText());
        }

        private static string AddBlockquotePrefix(string text)
        {
            // If line is blank or only whitespace, add blockquote prefix
            if (string.IsNullOrWhiteSpace(text))
            {
                return ">";
            }
            return text;
        }
    }
}
