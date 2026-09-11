using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly string _rootDirectory;
        private readonly Action<string> _directoryVisited;

        /// <summary>
        /// Creates a new scanner for the specified root directory.
        /// </summary>
        /// <param name="rootDirectory">The root directory to scan.</param>
        public MarkdownFileScanner(string rootDirectory)
            : this(rootDirectory, null)
        {
        }

        internal MarkdownFileScanner(string rootDirectory, Action<string> directoryVisited)
        {
            _rootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
            _directoryVisited = directoryVisited;

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

        }

        /// <summary>
        /// Scans the root directory for Markdown files, excluding ignored paths.
        /// </summary>
        /// <returns>List of absolute paths to Markdown files.</returns>
        public IReadOnlyList<string> ScanForMarkdownFiles(CancellationToken cancellationToken = default)
        {
            var markdownFiles = new List<string>();

            ScanDirectory(_rootDirectory, [], markdownFiles, cancellationToken);

            return markdownFiles;
        }

        /// <summary>
        /// Scans for Markdown files on a background thread.
        /// </summary>
        public Task<IReadOnlyList<string>> ScanForMarkdownFilesAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run(() => ScanForMarkdownFiles(cancellationToken), cancellationToken);
        }

        private void ScanDirectory(
            string directory,
            IReadOnlyList<IgnoreRule> inheritedRules,
            List<string> results,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _directoryVisited?.Invoke(directory);
                cancellationToken.ThrowIfCancellationRequested();

                // Check if this directory should be ignored by folder name
                var dirName = Path.GetFileName(directory);
                if (_ignoredFolderNames.Contains(dirName))
                    return;

                IReadOnlyList<IgnoreRule> rules = AddIgnoreRules(directory, inheritedRules);

                // Get all markdown files in this directory
                foreach (var file in Directory.EnumerateFiles(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var extension = Path.GetExtension(file);
                    if (IsMarkdownExtension(extension))
                    {
                        // Check if file matches ignore patterns
                        var relativePath = GetRelativePath(_rootDirectory, file);
                        if (!IsIgnored(relativePath, rules))
                        {
                            results.Add(file);
                        }
                    }
                }

                // Recursively scan subdirectories
                foreach (var subDir in Directory.EnumerateDirectories(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (ShouldTraverseDirectory(new DirectoryInfo(subDir).Attributes))
                    {
                        ScanDirectory(subDir, rules, results, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
            catch (DirectoryNotFoundException)
            {
                // Skip directories that no longer exist
            }
            catch (IOException)
            {
                // Skip directories that become unavailable during discovery.
            }
        }

        private bool IsMarkdownExtension(string extension)
        {
            return _markdownExtensions.Contains(extension);
        }

        private static bool IsIgnored(string relativePath, IReadOnlyList<IgnoreRule> rules)
        {
            if (rules.Count == 0)
                return false;

            // Normalize path separators for the matcher
            var normalizedPath = relativePath.Replace('\\', '/');

            var isIgnored = false;
            foreach (IgnoreRule rule in rules)
            {
                PatternMatchingResult result = rule.Matcher.Match(normalizedPath);
                if (result.HasMatches)
                {
                    isIgnored = !rule.IsNegation;
                }
            }

            return isIgnored;
        }

        private IReadOnlyList<IgnoreRule> AddIgnoreRules(
            string directory,
            IReadOnlyList<IgnoreRule> inheritedRules)
        {
            var ignoreFilePath = Path.Combine(directory, _ignoreFileName);

            if (!File.Exists(ignoreFilePath))
                return inheritedRules;

            var rules = new List<IgnoreRule>(inheritedRules);
            string relativeDirectory = string.Equals(
                Path.GetFullPath(_rootDirectory).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : GetRelativePath(_rootDirectory, directory)
                        .Replace('\\', '/')
                        .Trim('/');
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
                matcher.AddInclude(NormalizeGlobPattern(pattern, relativeDirectory));
                rules.Add(new IgnoreRule(matcher, isNegation));
            }

            return rules;
        }

        private static string NormalizeGlobPattern(string pattern, string relativeDirectory)
        {
            // Normalize path separators
            pattern = pattern.Replace('\\', '/');

            // A leading slash anchors the pattern to the directory containing the ignore file.
            bool isAnchored = pattern.StartsWith("/");
            if (isAnchored)
            {
                pattern = pattern.Substring(1);
            }

            // If pattern ends with /, it matches directories
            if (pattern.EndsWith("/"))
            {
                pattern += "**";
            }

            // If pattern doesn't contain /, it matches in any directory
            if (!isAnchored && !pattern.Contains("/"))
            {
                pattern = "**/" + pattern;
            }

            return string.IsNullOrEmpty(relativeDirectory)
                ? pattern
                : relativeDirectory + "/" + pattern;
        }

        internal static bool ShouldTraverseDirectory(FileAttributes attributes)
        {
            return (attributes & FileAttributes.ReparsePoint) == 0;
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
