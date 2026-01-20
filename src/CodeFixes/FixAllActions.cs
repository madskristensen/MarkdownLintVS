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

                case "MD022": // Blanks around headings
                case "MD031": // Blanks around fences
                case "MD032": // Blanks around lists
                case "MD058": // Blanks around tables
                    if (violation.Message.Contains("preceded") || violation.Message.Contains("before"))
                    {
                        edit.Insert(line.Start, Environment.NewLine);
                    }
                    else
                    {
                        edit.Insert(line.EndIncludingLineBreak, Environment.NewLine);
                    }
                    break;
            }
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

            // Fix MD022, MD031, MD032, MD058 - Blanks around headings/fences/lists/tables
            // Need to analyze violations to know which lines need blank lines
            var currentText = string.Join(Environment.NewLine, lines);
            var blankLineViolations = MarkdownLintAnalyzer.Instance
                .Analyze(currentText, filePath)
                .Where(v => v.Rule.Id == "MD022" || v.Rule.Id == "MD031" ||
                           v.Rule.Id == "MD032" || v.Rule.Id == "MD058")
                .OrderByDescending(v => v.LineNumber)
                .ToList();

            foreach (LintViolation violation in blankLineViolations)
            {
                if (violation.LineNumber < 0 || violation.LineNumber >= lines.Count)
                    continue;

                if (violation.Message.Contains("preceded") || violation.Message.Contains("before"))
                {
                    lines.Insert(violation.LineNumber, string.Empty);
                }
                else
                {
                    lines.Insert(violation.LineNumber + 1, string.Empty);
                }
                modified = true;
            }

            if (modified)
            {
                var newText = string.Join(Environment.NewLine, lines);

                using (ITextEdit edit = snapshot.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(0, snapshot.Length), newText);
                    edit.Apply();
                }
            }
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
