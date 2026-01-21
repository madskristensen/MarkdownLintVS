using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to normalize space after list marker (MD030).
    /// </summary>
    [FixForRule("MD030")]
    public class NormalizeListMarkerSpaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Use single space after list marker";

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            edit.Replace(line.Start, line.Length, GetFixedText());
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            text = Regex.Replace(text, @"^(\s*)([-*+])(\s{2,})(\S)", "$1$2 $4");
            text = Regex.Replace(text, @"^(\s*)(\d+\.)(\s{2,})(\S)", "$1$2 $4");
            return text;
        }
    }
}
