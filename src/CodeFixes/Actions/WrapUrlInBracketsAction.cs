using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to wrap a bare URL in angle brackets (MD034).
    /// </summary>
    public class WrapUrlInBracketsAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Wrap URL in angle brackets";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var url = Snapshot.GetText(Span);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, $"<{url}>");
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return $"<{Snapshot.GetText(Span)}>";
        }
    }
}
