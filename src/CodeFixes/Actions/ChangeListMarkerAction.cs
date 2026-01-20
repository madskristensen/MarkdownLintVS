using System.Text.RegularExpressions;
using System.Threading;
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

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var fixedText = ChangeMarker(line.GetText());

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return ChangeMarker(line.GetText());
        }

        private string ChangeMarker(string text)
        {
            return _listMarkerPattern.Replace(text, $"$1{targetMarker}$3");
        }
    }
}
