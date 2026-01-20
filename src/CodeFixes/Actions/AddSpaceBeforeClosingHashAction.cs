using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add space before closing hash on closed atx heading (MD020).
    /// </summary>
    public class AddSpaceBeforeClosingHashAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add space before closing #";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var fixedText = AddSpaceBeforeClosingHash(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return AddSpaceBeforeClosingHash(line.GetText());
        }

        private static string AddSpaceBeforeClosingHash(string text)
        {
            // Find closing hashes and add space before them
            // Pattern: content followed by # at end (without space)
            Match match = Regex.Match(text, @"^(#{1,6}\s+.+?)(\s*)(#{1,6})\s*$");
            if (match.Success)
            {
                var content = match.Groups[1].Value.TrimEnd();
                var closingHashes = match.Groups[3].Value;
                return content + " " + closingHashes;
            }
            return text;
        }
    }
}
