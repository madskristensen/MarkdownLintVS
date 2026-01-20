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

        public override void ApplyFix(ITextEdit edit)
        {
            ITextSnapshotLine startLine = Snapshot.GetLineFromPosition(Span.Start);
            var startLineNumber = startLine.LineNumber;
            var endLineNumber = FindListEndLine(startLineNumber);

            // Check if we need a blank line after
            if (endLineNumber < Snapshot.LineCount - 1)
            {
                ITextSnapshotLine lineAfter = Snapshot.GetLineFromLineNumber(endLineNumber + 1);
                if (!string.IsNullOrWhiteSpace(lineAfter.GetText()))
                {
                    ITextSnapshotLine endLine = Snapshot.GetLineFromLineNumber(endLineNumber);
                    edit.Insert(endLine.EndIncludingLineBreak, Environment.NewLine);
                }
            }

            // Check if we need a blank line before
            if (startLineNumber > 0)
            {
                ITextSnapshotLine lineBefore = Snapshot.GetLineFromLineNumber(startLineNumber - 1);
                if (!string.IsNullOrWhiteSpace(lineBefore.GetText()))
                {
                    edit.Insert(startLine.Start, Environment.NewLine);
                }
            }
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
