using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Adds markdownlint rule suppressions to the applicable .editorconfig file.
    /// </summary>
    internal static class EditorConfigSuppressionHelper
    {
        private static readonly Regex _sectionPattern = new(@"^\s*\[(?<pattern>[^]]+)\]\s*$", RegexOptions.Compiled);
        private static readonly Regex _propertyPattern = new(@"^(?<indent>\s*)md_(?<name>[A-Za-z0-9_-]+)\s*=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string FindTargetPath(string filePath, string solutionDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            var directory = File.Exists(filePath) ? Path.GetDirectoryName(filePath) : filePath;
            if (string.IsNullOrEmpty(directory))
                return null;

            directory = Path.GetFullPath(directory);
            var existing = FindExistingEditorConfig(directory);
            if (existing != null)
                return existing;

            var repositoryRoot = FindRepositoryRoot(directory);
            if (repositoryRoot != null)
                return Path.Combine(repositoryRoot, ".editorconfig");

            if (!string.IsNullOrWhiteSpace(solutionDirectory) && Directory.Exists(solutionDirectory))
                return Path.Combine(Path.GetFullPath(solutionDirectory), ".editorconfig");

            return null;
        }

        public static bool Suppress(string filePath, string ruleName, string solutionDirectory = null)
        {
            var targetPath = FindTargetPath(filePath, solutionDirectory);
            if (targetPath == null || string.IsNullOrWhiteSpace(ruleName))
                return false;

            var exists = File.Exists(targetPath);
            var text = exists ? File.ReadAllText(targetPath) : string.Empty;
            var newline = text.IndexOf("\r\n", StringComparison.Ordinal) >= 0 ? "\r\n" : "\n";
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n').ToList();
            if (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            var propertyName = "md_" + ruleName.Replace('-', '_');
            var propertyIndex = FindProperty(lines, propertyName, Path.GetFileName(filePath));
            if (propertyIndex >= 0)
            {
                var indent = _propertyPattern.Match(lines[propertyIndex]).Groups["indent"].Value;
                lines[propertyIndex] = $"{indent}{propertyName} = false";
            }
            else
            {
                var sectionIndex = FindApplicableSection(lines, Path.GetFileName(filePath));
                if (sectionIndex < 0)
                {
                    if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                        lines.Add(string.Empty);
                    lines.Add("[*.md]");
                    sectionIndex = lines.Count - 1;
                }

                var insertIndex = FindSectionEnd(lines, sectionIndex);
                lines.Insert(insertIndex, $"{propertyName} = false");
            }

            var updated = string.Join(newline, lines) + newline;
            if (updated == text)
                return false;

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            File.WriteAllText(targetPath, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }

        private static int FindProperty(IReadOnlyList<string> lines, string propertyName, string fileName)
        {
            var sectionIndex = -1;
            for (var i = 0; i < lines.Count; i++)
            {
                Match sectionMatch = _sectionPattern.Match(lines[i]);
                if (sectionMatch.Success)
                    sectionIndex = SectionMatches(sectionMatch.Groups["pattern"].Value, fileName) ? i : -2;

                Match propertyMatch = _propertyPattern.Match(lines[i]);
                if (propertyMatch.Success &&
                    propertyMatch.Groups["name"].Value.Replace('-', '_').Equals(propertyName.Substring(3), StringComparison.OrdinalIgnoreCase) &&
                    (sectionIndex == -1 || sectionIndex >= 0))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FindApplicableSection(IReadOnlyList<string> lines, string fileName)
        {
            var sectionIndex = -1;
            for (var i = 0; i < lines.Count; i++)
            {
                Match match = _sectionPattern.Match(lines[i]);
                if (match.Success && SectionMatches(match.Groups["pattern"].Value, fileName))
                    sectionIndex = i;
            }
            return sectionIndex;
        }

        private static int FindSectionEnd(IReadOnlyList<string> lines, int sectionIndex)
        {
            for (var i = sectionIndex + 1; i < lines.Count; i++)
            {
                if (_sectionPattern.IsMatch(lines[i]))
                {
                    while (i > sectionIndex + 1 && string.IsNullOrWhiteSpace(lines[i - 1]))
                        i--;
                    return i;
                }
            }
            return lines.Count;
        }

        private static bool SectionMatches(string pattern, string fileName)
        {
            foreach (var candidate in pattern.Split(','))
            {
                var value = candidate.Trim();
                if (value == "*")
                    return true;

                var regex = "^" + Regex.Escape(value).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                if (Regex.IsMatch(fileName, regex, RegexOptions.IgnoreCase))
                    return true;
            }
            return false;
        }

        private static string FindExistingEditorConfig(string directory)
        {
            for (var current = new DirectoryInfo(directory); current != null; current = current.Parent)
            {
                var path = Path.Combine(current.FullName, ".editorconfig");
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        private static string FindRepositoryRoot(string directory)
        {
            for (var current = new DirectoryInfo(directory); current != null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
                    return current.FullName;
            }
            return null;
        }
    }
}
