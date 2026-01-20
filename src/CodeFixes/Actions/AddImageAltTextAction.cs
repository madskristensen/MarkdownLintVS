using System.Threading;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add alt text to an image (MD045).
    /// </summary>
    public class AddImageAltTextAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add alt text placeholder";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);

            // Find the empty brackets and add placeholder
            var fixedText = text.Replace("![](", "![image](");

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);
            return text.Replace("![](", "![image](");
        }
    }
}
