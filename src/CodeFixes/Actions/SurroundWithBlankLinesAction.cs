using System;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes.Actions
{
    /// <summary>
    /// Fix action to surround a list with blank lines (MD032).
    /// </summary>
    public class SurroundWithBlankLinesAction(ITextSnapshot snapshot, Span span) : MarkdownFixAction(snapshot, span)
    {
        private static readonly Regex _listItemPattern = new(
            @"^\s*([-*+]|\d+\.)\s",
            RegexOptions.Compiled);

        public override string DisplayText => "Surround list with blank lines";

        /// <summary>
        /// Gets the line number that a blank line would be inserted BEFORE (at list start).
        /// Returns -1 if no blank line is needed before.
        /// Used for deduplication in Fix All operations.
        /// </summary>
        public int InsertBeforeLine
        {
            get
            {
                ITextSnapshotLine startLine = Snapshot.GetLineFromPosition(Span.Start);
                int startLineNumber = startLine.LineNumber;

                if (startLineNumber > 0)
                {
                    ITextSnapshotLine lineBefore = Snapshot.GetLineFromLineNumber(startLineNumber - 1);
                    if (!string.IsNullOrWhiteSpace(lineBefore.GetText()))
                    {
                        return startLineNumber;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// Gets the line number that a blank line would be inserted BEFORE (after list end).
        /// Returns -1 if no blank line is needed after.
        /// Used for deduplication in Fix All operations.
        /// </summary>
        public int InsertAfterListBeforeLine
        {
            get
            {
                ITextSnapshotLine startLine = Snapshot.GetLineFromPosition(Span.Start);
                int startLineNumber = startLine.LineNumber;
                int endLineNumber = FindListEndLine(startLineNumber);

                if (endLineNumber < Snapshot.LineCount - 1)
                {
                    ITextSnapshotLine lineAfter = Snapshot.GetLineFromLineNumber(endLineNumber + 1);
                    if (!string.IsNullOrWhiteSpace(lineAfter.GetText()))
                    {
                        return endLineNumber + 1;
                    }
                }

                return -1;
            }
        }

        /// <summary>
        /// Applies the fix, optionally skipping insertions that have already been handled.
        /// </summary>
        public void ApplyFix(ITextEdit edit, bool skipBefore, bool skipAfter)
        {
            ITextSnapshotLine startLine = Snapshot.GetLineFromPosition(Span.Start);
            int startLineNumber = startLine.LineNumber;
            int endLineNumber = FindListEndLine(startLineNumber);

            // Check if we need a blank line after
            if (!skipAfter && endLineNumber < Snapshot.LineCount - 1)
            {
                ITextSnapshotLine lineAfter = Snapshot.GetLineFromLineNumber(endLineNumber + 1);
                if (!string.IsNullOrWhiteSpace(lineAfter.GetText()))
                {
                    ITextSnapshotLine endLine = Snapshot.GetLineFromLineNumber(endLineNumber);
                    edit.Insert(endLine.EndIncludingLineBreak, Environment.NewLine);
                }
            }

            // Check if we need a blank line before
            if (!skipBefore && startLineNumber > 0)
            {
                ITextSnapshotLine lineBefore = Snapshot.GetLineFromLineNumber(startLineNumber - 1);
                if (!string.IsNullOrWhiteSpace(lineBefore.GetText()))
                {
                    edit.Insert(startLine.Start, Environment.NewLine);
                }
            }
        }

        public override void ApplyFix(ITextEdit edit)
        {
            ApplyFix(edit, skipBefore: false, skipAfter: false);
        }

        private int FindListEndLine(int startLineNumber)
        {
            var endLineNumber = startLineNumber;

            for (var i = startLineNumber; i < Snapshot.LineCount; i++)
            {
                ITextSnapshotLine line = Snapshot.GetLineFromLineNumber(i);
                var lineText = line.GetText();

                // Check if this line is part of the list (list item or continuation)
                if (_listItemPattern.IsMatch(lineText))
                {
                    endLineNumber = i;
                }
                else if (string.IsNullOrWhiteSpace(lineText))
                {
                    // Blank line - could be end of list or between items
                    // Check if next non-blank line is a list item
                    var foundNextListItem = false;
                    for (var j = i + 1; j < Snapshot.LineCount; j++)
                    {
                        var nextLineText = Snapshot.GetLineFromLineNumber(j).GetText();
                        if (string.IsNullOrWhiteSpace(nextLineText))
                            continue;
                        if (_listItemPattern.IsMatch(nextLineText))
                        {
                            foundNextListItem = true;
                        }
                        break;
                    }
                    if (!foundNextListItem)
                    {
                        break;
                    }
                }
                else if (lineText.StartsWith("  ") || lineText.StartsWith("\t"))
                {
                    // Indented continuation of list item
                    endLineNumber = i;
                }
                else
                {
                    // Non-list content, end of list
                    break;
                }
            }

            return endLineNumber;
        }

        protected override string GetFixedText()
        {
            ITextSnapshotLine line = Snapshot.GetLineFromPosition(Span.Start);
            return Environment.NewLine + line.GetText() + Environment.NewLine;
        }
    }
}
