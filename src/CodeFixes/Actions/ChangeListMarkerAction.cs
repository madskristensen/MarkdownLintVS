using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change unordered list marker style (MD004).
    /// </summary>
    public class ChangeListMarkerAction(ITextSnapshot snapshot, Span span, char targetMarker) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _listMarkerPattern = new(
            @"^(\s*)([-*+])(\s)",
            RegexOptions.Compiled);

        public override string DisplayText => $"Change list marker to '{targetMarker}'";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return _listMarkerPattern.Replace(line.GetText(), $"$1{targetMarker}$3");
        }
    }
}
