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
        private static readonly string[] _markdownExtensions = [".md", ".markdown", ".mdown", ".mkd", ".mkdn", ".mdwn", ".mdtxt", ".mdtext"];
        private static readonly string[] _defaultIgnoredFolders = ["node_modules", "vendor", ".git", "bin", "obj", "packages", "TestResults"];
        private const string _ignoreFileName = ".markdownlintignore";

        private readonly HashSet<string> _ignoredFolderNames;
        private readonly Matcher _ignoreMatcher;
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
            catch
            {
                ignoredFolders = _defaultIgnoredFolders;
            }

            _ignoredFolderNames = new HashSet<string>(ignoredFolders, StringComparer.OrdinalIgnoreCase);

            // Initialize the glob matcher with patterns from .markdownlintignore
            _ignoreMatcher = CreateIgnoreMatcher(rootDirectory);
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
                foreach (var file in Directory.GetFiles(directory))
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
                foreach (var subDir in Directory.GetDirectories(directory))
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
            return _markdownExtensions.Any(ext =>
                string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsIgnored(string relativePath)
        {
            if (_ignoreMatcher == null)
                return false;

            // Normalize path separators for the matcher
            var normalizedPath = relativePath.Replace('\\', '/');

            // Check if the path matches any ignore pattern
            PatternMatchingResult result = _ignoreMatcher.Match(normalizedPath);
            return result.HasMatches;
        }

        private static Matcher CreateIgnoreMatcher(string rootDirectory)
        {
            var ignoreFilePath = Path.Combine(rootDirectory, _ignoreFileName);

            if (!File.Exists(ignoreFilePath))
                return null;

            var matcher = new Matcher();
            var lines = File.ReadAllLines(ignoreFilePath);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Handle negation patterns (!)
                if (trimmed.StartsWith("!"))
                {
                    // Negation: exclude from ignore (i.e., include the file)
                    // The Matcher doesn't support negation directly, so we handle it differently
                    // For now, we'll just add it as an exclude pattern
                    // A more sophisticated implementation would track negations separately
                    continue; // Skip negation for simplicity; can be enhanced later
                }

                // Add the pattern
                matcher.AddInclude(NormalizeGlobPattern(trimmed));
            }

            return matcher;
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
