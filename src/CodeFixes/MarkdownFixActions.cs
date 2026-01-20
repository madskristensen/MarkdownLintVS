using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Base class for markdown lint fix actions.
    /// </summary>
    public abstract class MarkdownFixAction(ITextSnapshot snapshot, Span span) : ISuggestedAction
    {
        protected readonly ITextSnapshot Snapshot = snapshot;
        protected readonly Span Span = span;

        public abstract string DisplayText { get; }
        public virtual string IconAutomationText => null;
        public virtual ImageMoniker IconMoniker => default;
        public virtual string InputGestureText => null;
        public virtual bool HasActionSets => false;
        public virtual bool HasPreview => true;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            ITrackingSpan trackingSpan = Snapshot.CreateTrackingSpan(Span, SpanTrackingMode.EdgeExclusive);
            var previewText = GetFixedText();

            return Task.FromResult<object>(previewText);
        }

        public abstract void Invoke(CancellationToken cancellationToken);

        protected abstract string GetFixedText();

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Fix action to remove trailing whitespace from a line.
    /// </summary>
    public class RemoveTrailingWhitespaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove trailing whitespace";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var lineText = line.GetText();
            var trimmedText = lineText.TrimEnd();

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, trimmedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return line.GetText().TrimEnd();
        }
    }

    /// <summary>
    /// Fix action to replace tabs with spaces.
    /// </summary>
    public class ReplaceTabsWithSpacesAction(ITextSnapshot snapshot, Span span, int spacesPerTab = 4) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => $"Replace tab with {spacesPerTab} spaces";

        public override void Invoke(CancellationToken cancellationToken)
        {
            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, new string(' ', spacesPerTab));
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return new string(' ', spacesPerTab);
        }
    }

    /// <summary>
    /// Fix action to add a blank line before content.
    /// </summary>
    public class AddBlankLineBeforeAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add blank line before";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(line.Start, Environment.NewLine);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return Environment.NewLine + line.GetText();
        }
    }

    /// <summary>
    /// Fix action to add a blank line after content.
    /// </summary>
    public class AddBlankLineAfterAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add blank line after";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(line.EndIncludingLineBreak, Environment.NewLine);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return line.GetText() + Environment.NewLine;
        }
    }

    /// <summary>
    /// Fix action to remove extra blank lines.
    /// </summary>
    public class RemoveExtraBlankLinesAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove extra blank lines";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                // Delete the entire blank line including line break
                edit.Delete(line.Start, line.LengthIncludingLineBreak);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Fix action to add space after heading hash.
    /// </summary>
    public class AddSpaceAfterHashAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add space after #";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var hashEnd = text.LastIndexOf('#') + 1;

            // Find where hashes end
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '#')
                {
                    hashEnd = i;
                    break;
                }
            }

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(line.Start + hashEnd, " ");
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var hashEnd = 0;

            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '#')
                {
                    hashEnd = i;
                    break;
                }
            }

            return text.Substring(0, hashEnd) + " " + text.Substring(hashEnd);
        }
    }

    /// <summary>
    /// Fix action to remove multiple spaces (normalize to single space).
    /// </summary>
    public class NormalizeWhitespaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Use single space";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var normalized = System.Text.RegularExpressions.Regex.Replace(text, @"(\S)  +", "$1 ");

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, normalized);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            return System.Text.RegularExpressions.Regex.Replace(text, @"(\S)  +", "$1 ");
        }
    }

    /// <summary>
    /// Fix action to remove leading whitespace from a heading.
    /// </summary>
    public class RemoveLeadingWhitespaceAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove leading whitespace";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var trimmedText = text.TrimStart();

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, trimmedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return line.GetText().TrimStart();
        }
    }

    /// <summary>
    /// Fix action to remove trailing punctuation from a heading.
    /// </summary>
    public class RemoveTrailingPunctuationAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Remove trailing punctuation";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText().TrimEnd();

            // Remove trailing punctuation
            while (text.Length > 0 && ".,;:!。，；：！".Contains(text[text.Length - 1].ToString()))
            {
                text = text.Substring(0, text.Length - 1).TrimEnd();
            }

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, text);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText().TrimEnd();

            while (text.Length > 0 && ".,;:!。，；：！".Contains(text[text.Length - 1].ToString()))
            {
                text = text.Substring(0, text.Length - 1).TrimEnd();
            }

            return text;
        }
    }

    /// <summary>
    /// Fix action to add a newline at end of file.
    /// </summary>
    public class AddFinalNewlineAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add newline at end of file";

        public override void Invoke(CancellationToken cancellationToken)
        {
            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Insert(Snapshot.Length, Environment.NewLine);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return Environment.NewLine;
        }
    }

    /// <summary>
    /// Fix action to wrap a bare URL in angle brackets.
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

    /// <summary>
    /// Fix action to add alt text to an image.
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

    /// <summary>
    /// Fix action to add language to a fenced code block.
    /// </summary>
    public class AddCodeBlockLanguageAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Add language identifier";

        public override void Invoke(CancellationToken cancellationToken)
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            var fence = text.TrimStart();
            var indent = text.Length - fence.Length;
            var fenceChar = fence[0];
            var fenceLength = 0;

            for (var i = 0; i < fence.Length && fence[i] == fenceChar; i++)
                fenceLength++;

            var newText = new string(' ', indent) + new string(fenceChar, fenceLength) + "text";

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(line.Start, line.Length, newText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            var text = line.GetText();
            return text.TrimEnd() + "text";
        }
    }

    /// <summary>
    /// Fix action to swap reversed link syntax.
    /// </summary>
    public class FixReversedLinkAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        public override string DisplayText => "Fix reversed link syntax";

        public override void Invoke(CancellationToken cancellationToken)
        {
            var text = Snapshot.GetText(Span);
            var fixedText = FixReversedLink(text);

            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, fixedText);
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return FixReversedLink(Snapshot.GetText(Span));
        }

        private string FixReversedLink(string text)
        {
            // Convert (url)[text] to [text](url)
            Match match = System.Text.RegularExpressions.Regex.Match(text, @"\(([^)]+)\)\[([^\]]+)\]");
            if (match.Success)
            {
                return $"[{match.Groups[2].Value}]({match.Groups[1].Value})";
            }
            return text;
        }
    }
}
