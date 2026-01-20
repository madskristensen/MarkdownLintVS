using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Fix action to fix all instances of a specific rule in the document.
    /// </summary>
    public class FixAllInDocumentAction : ISuggestedAction
    {
        private readonly ITextSnapshot _snapshot;
        private readonly string _ruleId;
        private readonly string _filePath;

        public string DisplayText => $"Fix all {_ruleId} violations in document";
        public string IconAutomationText => null;
        public ImageMoniker IconMoniker => default;
        public string InputGestureText => null;
        public bool HasActionSets => false;
        public bool HasPreview => false;

        public FixAllInDocumentAction(ITextSnapshot snapshot, string ruleId, string filePath)
        {
            _snapshot = snapshot;
            _ruleId = ruleId;
            _filePath = filePath;
        }

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
            var text = _snapshot.GetText();
            var violations = Linting.MarkdownLintAnalyzer.Instance
                .Analyze(text, _filePath)
                .Where(v => v.Rule.Id == _ruleId)
                .OrderByDescending(v => v.LineNumber) // Process from bottom to top
                .ThenByDescending(v => v.ColumnStart)
                .ToList();

            if (violations.Count == 0)
                return;

            using (ITextEdit edit = _snapshot.TextBuffer.CreateEdit())
            {
                foreach (LintViolation violation in violations)
                {
                    ApplyFix(edit, violation);
                }
                edit.Apply();
            }
        }

        private void ApplyFix(ITextEdit edit, Linting.LintViolation violation)
        {
            if (violation.LineNumber >= _snapshot.LineCount)
                return;

            ITextSnapshotLine line = _snapshot.GetLineFromLineNumber(violation.LineNumber);
            var lineText = line.GetText();

            switch (_ruleId)
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
    public class FixAllAutoFixableAction : ISuggestedAction
    {
        private readonly ITextSnapshot _snapshot;
        private readonly string _filePath;

        private static readonly HashSet<string> AutoFixableRules =
        [
            "MD009", // Trailing spaces
            "MD010", // Hard tabs
            "MD012", // Multiple blank lines
            "MD018", // No space after hash
            "MD019", // Multiple spaces after hash
            "MD023", // Heading start left
            "MD027", // Multiple spaces after blockquote
        ];

        public string DisplayText => "Fix all auto-fixable violations in document";
        public string IconAutomationText => null;
        public ImageMoniker IconMoniker => default;
        public string InputGestureText => null;
        public bool HasActionSets => false;
        public bool HasPreview => false;

        public FixAllAutoFixableAction(ITextSnapshot snapshot, string filePath)
        {
            _snapshot = snapshot;
            _filePath = filePath;
        }

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
            var text = _snapshot.GetText();
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

            if (modified)
            {
                var newText = string.Join(Environment.NewLine, lines);
                
                using (ITextEdit edit = _snapshot.TextBuffer.CreateEdit())
                {
                    edit.Replace(new Span(0, _snapshot.Length), newText);
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
