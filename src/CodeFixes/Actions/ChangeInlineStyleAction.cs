using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Base class for fix actions that change inline text styles using regex replacement.
    /// Used for emphasis (MD049) and strong (MD050) style changes.
    /// </summary>
    public abstract class ChangeInlineStyleAction(ITextSnapshot snapshot, Span span, string targetStyle) 
        : MarkdownFixAction(snapshot, span)
    {
        protected string TargetStyle { get; } = targetStyle;

        /// <summary>
        /// Gets the display name for the style type (e.g., "emphasis", "strong").
        /// </summary>
        protected abstract string StyleTypeName { get; }

        /// <summary>
        /// Gets the regex pattern to find the style to replace when converting to asterisk.
        /// </summary>
        protected abstract string ToAsteriskPattern { get; }

        /// <summary>
        /// Gets the replacement string when converting to asterisk.
        /// </summary>
        protected abstract string ToAsteriskReplacement { get; }

        /// <summary>
        /// Gets the regex pattern to find the style to replace when converting to underscore.
        /// </summary>
        protected abstract string ToUnderscorePattern { get; }

        /// <summary>
        /// Gets the replacement string when converting to underscore.
        /// </summary>
        protected abstract string ToUnderscoreReplacement { get; }

        public override string DisplayText => $"Change {StyleTypeName} to {TargetStyle}";

        public override void ApplyFix(ITextEdit edit)
        {
            edit.Replace(Span, GetFixedText());
        }

        protected override string GetFixedText()
        {
            var text = Snapshot.GetText(Span);

            if (TargetStyle == "asterisk")
                return Regex.Replace(text, ToAsteriskPattern, ToAsteriskReplacement);
            else
                return Regex.Replace(text, ToUnderscorePattern, ToUnderscoreReplacement);
        }
    }
}
