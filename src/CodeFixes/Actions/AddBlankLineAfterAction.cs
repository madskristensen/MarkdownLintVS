using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to add a blank line after content (MD022, MD031, MD032, MD058).
    /// </summary>
    public class AddBlankLineAfterAction(ITextSnapshot snapshot, Span span)
        : BlankLineActionBase(snapshot, span, BlankLinePosition.After)
    {
    }
}
