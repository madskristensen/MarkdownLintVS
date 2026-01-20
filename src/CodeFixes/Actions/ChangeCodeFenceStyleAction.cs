using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to change code fence style between backtick and tilde (MD048).
    /// </summary>
    public class ChangeCodeFenceStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _backtickFencePattern = new(
            @"^(\s*)(`{3,})",
            RegexOptions.Compiled);

        private static readonly Regex _tildeFencePattern = new(
            @"^(\s*)(~{3,})",
            RegexOptions.Compiled);

        public override string DisplayText => $"Change code fence to {targetStyle}";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var fixedText = ChangeFenceStyle(line.GetText());

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return ChangeFenceStyle(line.GetText());
        }

        private string ChangeFenceStyle(string text)
        {
            if (targetStyle == "backtick")
            {
                // Change ~~~ to ```
                return _tildeFencePattern.Replace(text, m =>
                {
                    var indent = m.Groups[1].Value;
                    var fence = m.Groups[2].Value;
                    return indent + new string('`', fence.Length);
                });
            }
            else
            {
                // Change ``` to ~~~
                return _backtickFencePattern.Replace(text, m =>
                {
                    var indent = m.Groups[1].Value;
                    var fence = m.Groups[2].Value;
                    return indent + new string('~', fence.Length);
                });
            }
        }
    }
}
