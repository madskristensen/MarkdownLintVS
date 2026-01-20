using System.Text.RegularExpressions;
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

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return _orderedListPattern.Replace(line.GetText(), $"$1{expectedNumber}$3 ");
        }
    }
}
