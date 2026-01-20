using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Base class for markdown lint fix actions.
    /// </summary>
    public abstract class MarkdownFixAction : ISuggestedAction
    {
        protected readonly ITextSnapshot Snapshot;
        protected readonly Span Span;

        public abstract string DisplayText { get; }
        public virtual string IconAutomationText => null;
        public virtual ImageMoniker IconMoniker => default;
        public virtual string InputGestureText => null;
        public virtual bool HasActionSets => false;
        public virtual bool HasPreview => true;

        protected MarkdownFixAction(ITextSnapshot snapshot, Span span)
        {
            Snapshot = snapshot;
            Span = span;
        }

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
    public class RemoveTrailingWhitespaceAction : MarkdownFixAction
    {
        public override string DisplayText => "Remove trailing whitespace";

        public RemoveTrailingWhitespaceAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class ReplaceTabsWithSpacesAction : MarkdownFixAction
    {
        private readonly int _spacesPerTab;

        public override string DisplayText => $"Replace tab with {_spacesPerTab} spaces";

        public ReplaceTabsWithSpacesAction(ITextSnapshot snapshot, Span span, int spacesPerTab = 4)
            : base(snapshot, span)
        {
            _spacesPerTab = spacesPerTab;
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            using (ITextEdit edit = Snapshot.TextBuffer.CreateEdit())
            {
                edit.Replace(Span, new string(' ', _spacesPerTab));
                edit.Apply();
            }
        }

        protected override string GetFixedText()
        {
            return new string(' ', _spacesPerTab);
        }
    }

    /// <summary>
    /// Fix action to add a blank line before content.
    /// </summary>
    public class AddBlankLineBeforeAction : MarkdownFixAction
    {
        public override string DisplayText => "Add blank line before";

        public AddBlankLineBeforeAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class AddBlankLineAfterAction : MarkdownFixAction
    {
        public override string DisplayText => "Add blank line after";

        public AddBlankLineAfterAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class RemoveExtraBlankLinesAction : MarkdownFixAction
    {
        public override string DisplayText => "Remove extra blank lines";

        public RemoveExtraBlankLinesAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class AddSpaceAfterHashAction : MarkdownFixAction
    {
        public override string DisplayText => "Add space after #";

        public AddSpaceAfterHashAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class NormalizeWhitespaceAction : MarkdownFixAction
    {
        public override string DisplayText => "Use single space";

        public NormalizeWhitespaceAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class RemoveLeadingWhitespaceAction : MarkdownFixAction
    {
        public override string DisplayText => "Remove leading whitespace";

        public RemoveLeadingWhitespaceAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class RemoveTrailingPunctuationAction : MarkdownFixAction
    {
        public override string DisplayText => "Remove trailing punctuation";

        public RemoveTrailingPunctuationAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class AddFinalNewlineAction : MarkdownFixAction
    {
        public override string DisplayText => "Add newline at end of file";

        public AddFinalNewlineAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class WrapUrlInBracketsAction : MarkdownFixAction
    {
        public override string DisplayText => "Wrap URL in angle brackets";

        public WrapUrlInBracketsAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class AddImageAltTextAction : MarkdownFixAction
    {
        public override string DisplayText => "Add alt text placeholder";

        public AddImageAltTextAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class AddCodeBlockLanguageAction : MarkdownFixAction
    {
        public override string DisplayText => "Add language identifier";

        public AddCodeBlockLanguageAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
    public class FixReversedLinkAction : MarkdownFixAction
    {
        public override string DisplayText => "Fix reversed link syntax";

        public FixReversedLinkAction(ITextSnapshot snapshot, Span span)
            : base(snapshot, span)
        {
        }

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
