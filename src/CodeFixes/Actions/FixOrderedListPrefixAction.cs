using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to fix ordered list item prefix (MD029).
    /// Changes the number prefix to match the expected style.
    /// </summary>
    public class FixOrderedListPrefixAction(ITextSnapshot snapshot, Span span, int expectedNumber) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _orderedListPattern = new(
            @"^(\s*)(\d+)(\.)\s",
            RegexOptions.Compiled);

        public override string DisplayText => $"Change list prefix to '{expectedNumber}.'";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var fixedText = FixPrefix(line.GetText());

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return FixPrefix(line.GetText());
        }

        private string FixPrefix(string text)
        {
            return _orderedListPattern.Replace(text, $"$1{expectedNumber}$3 ");
        }
    }
}
