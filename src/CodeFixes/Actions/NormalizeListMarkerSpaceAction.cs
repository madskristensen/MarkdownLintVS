using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to normalize space after list marker (MD030).
    /// </summary>
    public class NormalizeListMarkerSpaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Use single space after list marker";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var fixedText = NormalizeListMarkerSpace(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return NormalizeListMarkerSpace(line.GetText());
        }

        private static string NormalizeListMarkerSpace(string text)
        {
            // Normalize unordered list markers: -, *, +
            text = Regex.Replace(text, @"^(\s*)([-*+])(\s{2,})(\S)", "$1$2 $4");
            // Normalize ordered list markers: 1., 2., etc.
            text = Regex.Replace(text, @"^(\s*)(\d+\.)(\s{2,})(\S)", "$1$2 $4");
            return text;
        }
    }
}
