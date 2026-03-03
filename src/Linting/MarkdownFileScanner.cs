using System.Collections.Generic;
using System.IO;
using System.Linq;
using MarkdownLintVS.Options;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Scans directories for Markdown files, respecting ignore patterns.
    /// </summary>
    public class MarkdownFileScanner
    {
        private sealed class IgnoreRule(Matcher matcher, bool isNegation)
        {
            public Matcher Matcher { get; } = matcher;
            public bool IsNegation { get; } = isNegation;
        }

        private static readonly HashSet<string> _markdownExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".markdown", ".mdown", ".mkd", ".mkdn", ".mdwn", ".mdtxt", ".mdtext"
        };
        private static readonly string[] _defaultIgnoredFolders = ["node_modules", "vendor", ".git", "bin", "obj", "packages", "TestResults"];
        private const string _ignoreFileName = ".markdownlintignore";

        private readonly HashSet<string> _ignoredFolderNames;
        private readonly List<IgnoreRule> _ignoreRules;
        private readonly string _rootDirectory;

        /// <summary>
        /// Creates a new scanner for the specified root directory.
        /// </summary>
        /// <param name="rootDirectory">The root directory to scan.</param>
        public MarkdownFileScanner(string rootDirectory)
        {
            _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));

            // Get ignored folder names from settings (with fallback to defaults)
            string[] ignoredFolders;
            try
            {
                ignoredFolders = GeneralOptions.Instance?.GetIgnoredFolderNames() ?? _defaultIgnoredFolders;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load Markdown lint ignored folders from options: {ex.Message}");
                ignoredFolders = _defaultIgnoredFolders;
            }

            _ignoredFolderNames = new HashSet<string>(ignoredFolders, StringComparer.OrdinalIgnoreCase);

            // Initialize the glob matcher with patterns from .markdownlintignore
            _ignoreRules = CreateIgnoreRules(rootDirectory);
        }

        /// <summary>
        /// Scans the root directory for Markdown files, excluding ignored paths.
        /// </summary>
        /// <returns>List of absolute paths to Markdown files.</returns>
        public IReadOnlyList<string> ScanForMarkdownFiles()
        {
            var markdownFiles = new List<string>();

            ScanDirectory(_rootDirectory, markdownFiles);

            return markdownFiles;
        }

        private void ScanDirectory(string directory, List<string> results)
        {
            try
            {
                // Check if this directory should be ignored by folder name
                var dirName = Path.GetFileName(directory);
                if (_ignoredFolderNames.Contains(dirName))
                    return;

                // Get all markdown files in this directory
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    var extension = Path.GetExtension(file);
                    if (IsMarkdownExtension(extension))
                    {
                        // Check if file matches ignore patterns
                        var relativePath = GetRelativePath(_rootDirectory, file);
                        if (!IsIgnored(relativePath))
                        {
                            results.Add(file);
                        }
                    }
                }

                // Recursively scan subdirectories
                foreach (var subDir in Directory.EnumerateDirectories(directory))
                {
                    ScanDirectory(subDir, results);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
            catch (DirectoryNotFoundException)
            {
                // Skip directories that no longer exist
            }
        }

        private bool IsMarkdownExtension(string extension)
        {
            return _markdownExtensions.Contains(extension);
        }

        private bool IsIgnored(string relativePath)
        {
            if (_ignoreRules == null || _ignoreRules.Count == 0)
                return false;

            // Normalize path separators for the matcher
            var normalizedPath = relativePath.Replace('\\', '/');

            var isIgnored = false;
            foreach (IgnoreRule rule in _ignoreRules)
            {
                PatternMatchingResult result = rule.Matcher.Match(normalizedPath);
                if (result.HasMatches)
                {
                    isIgnored = !rule.IsNegation;
                }
            }

            return isIgnored;
        }

        private static List<IgnoreRule> CreateIgnoreRules(string rootDirectory)
        {
            var ignoreFilePath = Path.Combine(rootDirectory, _ignoreFileName);

            if (!File.Exists(ignoreFilePath))
                return [];

            var rules = new List<IgnoreRule>();
            var lines = File.ReadAllLines(ignoreFilePath);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                var isNegation = trimmed.StartsWith("!");
                var pattern = isNegation ? trimmed.Substring(1).Trim() : trimmed;
                if (string.IsNullOrEmpty(pattern))
                {
                    continue;
                }

                var matcher = new Matcher();
                matcher.AddInclude(NormalizeGlobPattern(pattern));
                rules.Add(new IgnoreRule(matcher, isNegation));
            }

            return rules;
        }

        private static string NormalizeGlobPattern(string pattern)
        {
            // Normalize path separators
            pattern = pattern.Replace('\\', '/');

            // If pattern starts with /, it's relative to root
            if (pattern.StartsWith("/"))
            {
                pattern = pattern.Substring(1);
            }

            // If pattern ends with /, it matches directories
            if (pattern.EndsWith("/"))
            {
                pattern += "**";
            }

            // If pattern doesn't contain /, it matches in any directory
            if (!pattern.Contains("/"))
            {
                pattern = "**/" + pattern;
            }

            return pattern;
        }

        private static string GetRelativePath(string basePath, string fullPath)
        {
            // Ensure paths end with separator for proper comparison
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                basePath += Path.DirectorySeparatorChar;
            }

            var baseUri = new Uri(basePath);
            var fullUri = new Uri(fullPath);

            Uri relativeUri = baseUri.MakeRelativeUri(fullUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Gets the total count of Markdown files found (for progress reporting).
        /// </summary>
        public static int CountMarkdownFiles(string directory)
        {
            var scanner = new MarkdownFileScanner(directory);
            return scanner.ScanForMarkdownFiles().Count;
        }
    }
}
