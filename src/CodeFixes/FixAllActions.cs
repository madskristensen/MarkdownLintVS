using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Fix action to fix all instances of a specific rule in the document.
    /// </summary>
    public class FixAllInDocumentAction(ITextSnapshot snapshot, string ruleId, string filePath) : ISuggestedAction
    {
        public string DisplayText => $"Fix all {ruleId} violations in document";
        public string IconAutomationText => null;
        public ImageMoniker IconMoniker => default;
        public string InputGestureText => null;
        public bool HasActionSets => false;
        public bool HasPreview => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            var text = snapshot.GetText();
            var violations = Linting.MarkdownLintAnalyzer.Instance
                .Analyze(text, filePath)
                .Where(v => v.Rule.Id == ruleId)
                .OrderByDescending(v => v.LineNumber) // Process from bottom to top
                .ThenByDescending(v => v.ColumnStart)
                .ToList();

            if (violations.Count == 0)
                return;

            using (ITextEdit edit = snapshot.TextBuffer.CreateEdit())
            {
                foreach (LintViolation violation in violations)
                {
                    ApplyFix(edit, violation);
                }
                edit.Apply();
            }
        }

        private void ApplyFix(ITextEdit edit, LintViolation violation)
        {
            if (violation.LineNumber >= snapshot.LineCount)
                return;

            ITextSnapshotLine line = snapshot.GetLineFromLineNumber(violation.LineNumber);
            var lineText = line.GetText();

            switch (ruleId)
            {
                case "MD009": // Trailing spaces
                    var trimmed = lineText.TrimEnd();
                    if (trimmed != lineText)
                    {
                        edit.Replace(line.Start, line.Length, trimmed);
                    }
                    break;

                case "MD010": // Hard tabs
                    if (violation.ColumnStart < lineText.Length && lineText[violation.ColumnStart] == '\t')
                    {
                        edit.Replace(line.Start + violation.ColumnStart, 1, "    ");
                    }
                    break;

                case "MD012": // Multiple blank lines
                    if (string.IsNullOrWhiteSpace(lineText))
                    {
                        edit.Delete(line.Start, line.LengthIncludingLineBreak);
                    }
                    break;

                case "MD018": // No space after hash
                    Match hashMatch = Regex.Match(lineText, @"^(#{1,6})(\S)");
                    if (hashMatch.Success)
                    {
                        SnapshotPoint insertPos = line.Start + hashMatch.Groups[1].Length;
                        edit.Insert(insertPos, " ");
                    }
                    break;

                case "MD019": // Multiple spaces after hash
                    Match multiSpaceMatch = Regex.Match(lineText, @"^(#{1,6})\s{2,}");
                    if (multiSpaceMatch.Success)
                    {
                        SnapshotPoint start = line.Start + multiSpaceMatch.Groups[1].Length;
                        var length = multiSpaceMatch.Length - multiSpaceMatch.Groups[1].Length;
                        edit.Replace(start, length, " ");
                    }
                    break;

                case "MD023": // Heading start left
                    var leadingWs = lineText.Length - lineText.TrimStart().Length;
                    if (leadingWs > 0)
                    {
                        edit.Delete(line.Start, leadingWs);
                    }
                    break;

                case "MD026": // Trailing punctuation
                    Match punctMatch = Regex.Match(lineText, @"[.,;:!。，；：！]+\s*$");
                    if (punctMatch.Success)
                    {
                        edit.Delete(line.Start + punctMatch.Index, punctMatch.Length);
                    }
                    break;

                case "MD027": // Multiple spaces after blockquote
                    Match bqMatch = Regex.Match(lineText, @"^(\s*>+)\s{2,}");
                    if (bqMatch.Success)
                    {
                        SnapshotPoint start = line.Start + bqMatch.Groups[1].Length;
                        var length = bqMatch.Length - bqMatch.Groups[1].Length;
                        edit.Replace(start, length, " ");
                    }
                    break;

                case "MD011": // Reversed links
                    Match reversedMatch = Regex.Match(lineText.Substring(violation.ColumnStart), @"\(([^)]+)\)\[([^\]]+)\]");
                    if (reversedMatch.Success)
                    {
                        SnapshotPoint startPos = line.Start + violation.ColumnStart + reversedMatch.Index;
                        edit.Replace(startPos, reversedMatch.Length, $"[{reversedMatch.Groups[2].Value}]({reversedMatch.Groups[1].Value})");
                    }
                    break;

                case "MD014": // Dollar signs before commands
                    Match dollarMatch = Regex.Match(lineText, @"^\$\s?");
                    if (dollarMatch.Success)
                    {
                        edit.Delete(line.Start, dollarMatch.Length);
                    }
                    break;

                case "MD020": // No space inside closed atx
                    Match closedAtxMatch = Regex.Match(lineText, @"^(#{1,6}\s+.+?)(\s*)(#{1,6})\s*$");
                    if (closedAtxMatch.Success)
                    {
                        var content = closedAtxMatch.Groups[1].Value.TrimEnd();
                        var closingHashes = closedAtxMatch.Groups[3].Value;
                        edit.Replace(line.Start, line.Length, content + " " + closingHashes);
                    }
                    break;

                case "MD021": // Multiple spaces in closed atx
                    var normalizedClosedAtx = Regex.Replace(lineText, @"(\S)  +", "$1 ");
                    if (normalizedClosedAtx != lineText)
                    {
                        edit.Replace(line.Start, line.Length, normalizedClosedAtx);
                    }
                    break;

                case "MD030": // Spaces after list markers
                    var normalizedList = Regex.Replace(lineText, @"^(\s*)([-*+])(\s{2,})(\S)", "$1$2 $4");
                    normalizedList = Regex.Replace(normalizedList, @"^(\s*)(\d+\.)(\s{2,})(\S)", "$1$2 $4");
                    if (normalizedList != lineText)
                    {
                        edit.Replace(line.Start, line.Length, normalizedList);
                    }
                    break;

                case "MD034": // Bare URLs
                    if (violation.ColumnStart >= 0 && violation.ColumnEnd > violation.ColumnStart)
                    {
                        var urlLength = violation.ColumnEnd - violation.ColumnStart;
                        var url = lineText.Substring(violation.ColumnStart, urlLength);
                        edit.Replace(line.Start + violation.ColumnStart, urlLength, $"<{url}>");
                    }
                    break;

                case "MD037": // Spaces inside emphasis
                    if (violation.ColumnStart >= 0 && violation.ColumnEnd > violation.ColumnStart)
                    {
                        var emphasisLength = violation.ColumnEnd - violation.ColumnStart;
                        var emphasisText = lineText.Substring(violation.ColumnStart, emphasisLength);
                        var fixedEmphasis = Regex.Replace(emphasisText, @"(\*+|_+)\s+", "$1");
                        fixedEmphasis = Regex.Replace(fixedEmphasis, @"\s+(\*+|_+)$", "$1");
                        if (fixedEmphasis != emphasisText)
                        {
                            edit.Replace(line.Start + violation.ColumnStart, emphasisLength, fixedEmphasis);
                        }
                    }
                    break;

                case "MD038": // Spaces inside code span
                    if (violation.ColumnStart >= 0 && violation.ColumnEnd > violation.ColumnStart)
                    {
                        var codeLength = violation.ColumnEnd - violation.ColumnStart;
                        var codeText = lineText.Substring(violation.ColumnStart, codeLength);
                        var backtickCount = 0;
                        for (var i = 0; i < codeText.Length && codeText[i] == '`'; i++)
                            backtickCount++;
                        if (backtickCount > 0 && codeText.Length > backtickCount * 2)
                        {
                            var delimiter = new string('`', backtickCount);
                            var content = codeText.Substring(backtickCount, codeText.Length - backtickCount * 2);
                            edit.Replace(line.Start + violation.ColumnStart, codeLength, delimiter + content.Trim() + delimiter);
                        }
                    }
                    break;

                case "MD039": // Spaces inside link text
                    if (violation.ColumnStart >= 0 && violation.ColumnEnd > violation.ColumnStart)
                    {
                        var linkLength = violation.ColumnEnd - violation.ColumnStart;
                        var linkText = lineText.Substring(violation.ColumnStart, linkLength);
                        Match linkMatch = Regex.Match(linkText, @"^\[(\s*)([^\]]*?)(\s*)\](.*)$");
                        if (linkMatch.Success)
                        {
                            var content = linkMatch.Groups[2].Value;
                            var rest = linkMatch.Groups[4].Value;
                            edit.Replace(line.Start + violation.ColumnStart, linkLength, "[" + content + "]" + rest);
                        }
                    }
                    break;

                case "MD040": // Fenced code language
                    Match fenceMatch = Regex.Match(lineText, @"^(\s*)(```|~~~)\s*$");
                    if (fenceMatch.Success)
                    {
                        var indent = fenceMatch.Groups[1].Value;
                        var fence = fenceMatch.Groups[2].Value;
                        edit.Replace(line.Start, line.Length, indent + fence + "text");
                    }
                    break;

                case "MD045": // No alt text
                    if (violation.ColumnStart >= 0 && violation.ColumnEnd > violation.ColumnStart)
                    {
                        var imgLength = violation.ColumnEnd - violation.ColumnStart;
                        var imgText = lineText.Substring(violation.ColumnStart, imgLength);
                        var fixedImg = imgText.Replace("![](", "![image](");
                        if (fixedImg != imgText)
                        {
                            edit.Replace(line.Start + violation.ColumnStart, imgLength, fixedImg);
                        }
                    }
                    break;

                case "MD047": // Single trailing newline
                    if (violation.Message.Contains("multiple"))
                    {
                        // Remove extra trailing newlines - delete from current position to end
                        if (line.LineNumber > 0)
                        {
                            edit.Delete(line.Start, line.LengthIncludingLineBreak);
                        }
                    }
                    else
                    {
                        // Add missing trailing newline
                        edit.Insert(snapshot.Length, Environment.NewLine);
                    }
                    break;

                case "MD022": // Blanks around headings
                case "MD031": // Blanks around fences
                case "MD058": // Blanks around tables
                    // Check FixDescription for "before"/"after" since Message is the same for both cases
                    var fixDesc = violation.FixDescription ?? violation.Message;
                    if (fixDesc.Contains("before"))
                    {
                        edit.Insert(line.Start, Environment.NewLine);
                    }
                    else if (fixDesc.Contains("after"))
                    {
                        edit.Insert(line.EndIncludingLineBreak, Environment.NewLine);
                    }
                    break;

                case "MD032": // Blanks around lists
                    // Find the end of the list and add blank lines before and after as needed
                    var listEndLine = FindListEndLine(snapshot, violation.LineNumber);

                    // Add blank line after list if needed
                    if (listEndLine < snapshot.LineCount - 1)
                    {
                        ITextSnapshotLine lineAfterList = snapshot.GetLineFromLineNumber(listEndLine + 1);
                        if (!string.IsNullOrWhiteSpace(lineAfterList.GetText()))
                        {
                            ITextSnapshotLine endLine = snapshot.GetLineFromLineNumber(listEndLine);
                            edit.Insert(endLine.EndIncludingLineBreak, Environment.NewLine);
                        }
                    }

                    // Add blank line before list if needed
                    if (violation.LineNumber > 0)
                    {
                        ITextSnapshotLine lineBefore = snapshot.GetLineFromLineNumber(violation.LineNumber - 1);
                        if (!string.IsNullOrWhiteSpace(lineBefore.GetText()))
                        {
                            edit.Insert(line.Start, Environment.NewLine);
                        }
                    }
                    break;
            }
        }

        private static readonly Regex _listItemPattern = new(
            @"^\s*([-*+]|\d+\.)\s",
            RegexOptions.Compiled);

        private static int FindListEndLine(ITextSnapshot snapshot, int startLineNumber)
        {
            var endLineNumber = startLineNumber;

            for (var i = startLineNumber; i < snapshot.LineCount; i++)
            {
                ITextSnapshotLine line = snapshot.GetLineFromLineNumber(i);
                var lineText = line.GetText();

                if (_listItemPattern.IsMatch(lineText))
                {
                    endLineNumber = i;
                }
                else if (string.IsNullOrWhiteSpace(lineText))
                {
                    // Check if next non-blank line is a list item
                    var foundNextListItem = false;
                    for (var j = i + 1; j < snapshot.LineCount; j++)
                    {
                        var nextLineText = snapshot.GetLineFromLineNumber(j).GetText();
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
                    endLineNumber = i;
                }
                else
                {
                    break;
                }
            }

            return endLineNumber;
        }

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
    /// Fix action to fix all auto-fixable violations in the document.
    /// </summary>
    public class FixAllAutoFixableAction(ITextSnapshot snapshot, string filePath) : ISuggestedAction
    {
        public string DisplayText => "Fix all auto-fixable violations in document";
        public string IconAutomationText => null;
        public ImageMoniker IconMoniker => default;
        public string InputGestureText => null;
        public bool HasActionSets => false;
        public bool HasPreview => false;

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<SuggestedActionSet>>(null);
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(null);
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            var text = snapshot.GetText();
            List<string> lines = [.. text.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)];
            var modified = false;

            // Fix MD009 - Trailing spaces (all lines)
            for (var i = 0; i < lines.Count; i++)
            {
                var trimmed = lines[i].TrimEnd();
                if (trimmed != lines[i])
                {
                    lines[i] = trimmed;
                    modified = true;
                }
            }

            // Fix MD010 - Hard tabs (all lines)
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains('\t'))
                {
                    lines[i] = lines[i].Replace("\t", "    ");
                    modified = true;
                }
            }

            // Fix MD012 - Multiple blank lines
            var newLines = new List<string>();
            var consecutiveBlanks = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    consecutiveBlanks++;
                    if (consecutiveBlanks <= 1)
                    {
                        newLines.Add(line);
                    }
                    else
                    {
                        modified = true;
                    }
                }
                else
                {
                    consecutiveBlanks = 0;
                    newLines.Add(line);
                }
            }
            lines = newLines;

            // Fix MD018 - No space after hash
            for (var i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], @"^(#{1,6})([^#\s])");
                if (match.Success)
                {
                    lines[i] = match.Groups[1].Value + " " + lines[i].Substring(match.Groups[1].Length);
                    modified = true;
                }
            }

            // Fix MD019 - Multiple spaces after hash
            for (var i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], @"^(#{1,6})\s{2,}");
                if (match.Success)
                {
                    lines[i] = match.Groups[1].Value + " " + lines[i].Substring(match.Length);
                    modified = true;
                }
            }

            // Fix MD023 - Heading start left
            for (var i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], @"^\s+(#{1,6}\s)");
                if (match.Success)
                {
                    lines[i] = lines[i].TrimStart();
                    modified = true;
                }
            }

            // Fix MD027 - Multiple spaces after blockquote
            for (var i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], @"^(\s*>+)\s{2,}");
                if (match.Success)
                {
                    lines[i] = match.Groups[1].Value + " " + lines[i].Substring(match.Length);
                    modified = true;
                }
            }

            // Fix MD014 - Dollar signs before commands
            for (var i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], @"^\$\s?");
                if (match.Success)
                {
                    lines[i] = lines[i].Substring(match.Length);
                    modified = true;
                }
            }

            // Fix MD020 - No space inside closed atx
            for (var i = 0; i < lines.Count; i++)
            {
                Match match = Regex.Match(lines[i], @"^(#{1,6}\s+.+?)(\s*)(#{1,6})\s*$");
                if (match.Success && lines[i].LastIndexOf('#') > 0)
                {
                    var lastHash = lines[i].LastIndexOf('#');
                    if (lastHash > 0 && lines[i][lastHash - 1] != ' ')
                    {
                        var content = match.Groups[1].Value.TrimEnd();
                        var closingHashes = match.Groups[3].Value;
                        lines[i] = content + " " + closingHashes;
                        modified = true;
                    }
                }
            }

            // Fix MD021 - Multiple spaces in closed atx
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Contains("#") && lines[i].TrimEnd().EndsWith("#"))
                {
                    var normalized = Regex.Replace(lines[i], @"(\S)  +", "$1 ");
                    if (normalized != lines[i])
                    {
                        lines[i] = normalized;
                        modified = true;
                    }
                }
            }

            // Fix MD030 - Spaces after list markers
            for (var i = 0; i < lines.Count; i++)
            {
                var original = lines[i];
                lines[i] = Regex.Replace(lines[i], @"^(\s*)([-*+])(\s{2,})(\S)", "$1$2 $4");
                lines[i] = Regex.Replace(lines[i], @"^(\s*)(\d+\.)(\s{2,})(\S)", "$1$2 $4");
                if (lines[i] != original)
                {
                    modified = true;
                }
            }

            // Fix MD022, MD031, MD058 - Blanks around headings/fences/tables
            // Need to analyze violations to know which lines need blank lines
            var currentText = string.Join(Environment.NewLine, lines);
            var blankLineViolations = MarkdownLintAnalyzer.Instance
                .Analyze(currentText, filePath)
                .Where(v => v.Rule.Id == "MD022" || v.Rule.Id == "MD031" || v.Rule.Id == "MD058")
                .OrderByDescending(v => v.LineNumber)
                .ToList();

            foreach (LintViolation violation in blankLineViolations)
            {
                if (violation.LineNumber < 0 || violation.LineNumber >= lines.Count)
                    continue;

                // Check FixDescription for "before"/"after" since Message is the same for both cases
                var fixDesc = violation.FixDescription ?? violation.Message;
                if (fixDesc.Contains("before"))
                {
                    lines.Insert(violation.LineNumber, string.Empty);
                }
                else if (fixDesc.Contains("after"))
                {
                    lines.Insert(violation.LineNumber + 1, string.Empty);
                }
                modified = true;
            }

            // Fix MD032 - Blanks around lists (handled separately because it surrounds the list)
            currentText = string.Join(Environment.NewLine, lines);
            var listViolations = MarkdownLintAnalyzer.Instance
                .Analyze(currentText, filePath)
                .Where(v => v.Rule.Id == "MD032")
                .OrderByDescending(v => v.LineNumber)
                .ToList();

            foreach (LintViolation violation in listViolations)
            {
                if (violation.LineNumber < 0 || violation.LineNumber >= lines.Count)
                    continue;

                var startLine = violation.LineNumber;
                var endLine = FindListEndLineInLines(lines, startLine);

                // Add blank line after list if needed
                if (endLine < lines.Count - 1 && !string.IsNullOrWhiteSpace(lines[endLine + 1]))
                {
                    lines.Insert(endLine + 1, string.Empty);
                    modified = true;
                }

                // Add blank line before list if needed
                if (startLine > 0 && !string.IsNullOrWhiteSpace(lines[startLine - 1]))
                {
                    lines.Insert(startLine, string.Empty);
                    modified = true;
                }
            }

            // Fix MD047 - Single trailing newline
            // Remove trailing blank lines, then ensure exactly one newline at end
            while (lines.Count > 1 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
            {
                lines.RemoveAt(lines.Count - 1);
                modified = true;
            }
            // Ensure file ends with content (the final newline is added by string.Join behavior)
            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0 && lines.Count > 1)
            {
                // If last line is empty but we have content, that's fine - represents trailing newline
            }

            if (modified)
            {
                var newText = string.Join(Environment.NewLine, lines);
                // Ensure single trailing newline
                if (!newText.EndsWith(Environment.NewLine))
                {
                    newText += Environment.NewLine;
                }

                using (ITextEdit edit = snapshot.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(0, snapshot.Length), newText);
                    edit.Apply();
                }
            }
        }

        private static readonly Regex _listItemPatternForLines = new(
            @"^\s*([-*+]|\d+\.)\s",
            RegexOptions.Compiled);

        private static int FindListEndLineInLines(List<string> lines, int startLineNumber)
        {
            var endLineNumber = startLineNumber;

            for (var i = startLineNumber; i < lines.Count; i++)
            {
                var lineText = lines[i];

                if (_listItemPatternForLines.IsMatch(lineText))
                {
                    endLineNumber = i;
                }
                else if (string.IsNullOrWhiteSpace(lineText))
                {
                    // Check if next non-blank line is a list item
                    var foundNextListItem = false;
                    for (var j = i + 1; j < lines.Count; j++)
                    {
                        var nextLineText = lines[j];
                        if (string.IsNullOrWhiteSpace(nextLineText))
                            continue;
                        if (_listItemPatternForLines.IsMatch(nextLineText))
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
                    endLineNumber = i;
                }
                else
                {
                    break;
                }
            }

            return endLineNumber;
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        public void Dispose()
        {
        }
    }
}
