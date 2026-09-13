using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;
using Markdig.Syntax.Inlines;

namespace MarkdownLintVS.Linting.Rules
{
    /// <summary>
    /// MD061: File links should reference existing files.
    /// Validates that relative links to local files point to files that actually exist.
    /// Supports root-relative paths by using a configured root or searching parent directories.
    /// </summary>
    public class MD061_FileLinkExists : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD061");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(analysis.FilePath))
                yield break;

            var baseDirectory = Path.GetDirectoryName(analysis.FilePath);
            if (string.IsNullOrEmpty(baseDirectory))
                yield break;

            foreach (LinkInline link in analysis.GetLinks())
            {
                // Skip image links - those are handled by MD062
                if (link.IsImage)
                    continue;

                var url = link.Url;
                if (string.IsNullOrEmpty(url))
                    continue;

                // Skip external URLs
                if (IsExternalUrl(url))
                    continue;

                // Skip data URLs and javascript
                if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if the local file exists
                if (!LocalPathResolver.Exists(
                    analysis,
                    url,
                    baseDirectory,
                    analysis.RootPath,
                    allowDirectory: true,
                    allowMarkdownSibling: true))
                {
                    (var line, var column) = analysis.GetPositionFromOffset(link.Span.Start);
                    var cleanUrl = GetPathWithoutFragment(url);

                    yield return CreateViolation(
                        line,
                        column,
                        column + link.Span.Length,
                        $"Link references non-existent file: '{cleanUrl}'",
                        severity);
                }
            }
        }

        private static bool IsExternalUrl(string url)
        {
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("//", StringComparison.Ordinal);
        }

        private static string GetPathWithoutFragment(string url)
        {
            var fragmentIndex = url.IndexOf('#');
            return fragmentIndex >= 0 ? url.Substring(0, fragmentIndex) : url;
        }
    }

    /// <summary>
    /// MD062: Image links should reference existing files.
    /// Validates that relative image links point to files that actually exist.
    /// Supports root-relative paths by using a configured root or searching parent directories.
    /// </summary>
    public class MD062_ImageLinkExists : MarkdownRuleBase
    {
        private static readonly RuleInfo _info = RuleRegistry.GetRule("MD062");
        public override RuleInfo Info => _info;

        public override IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            RuleConfiguration configuration,
            DiagnosticSeverity severity,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(analysis.FilePath))
                yield break;

            var baseDirectory = Path.GetDirectoryName(analysis.FilePath);
            if (string.IsNullOrEmpty(baseDirectory))
                yield break;

            foreach (LinkInline link in analysis.GetLinks())
            {
                // Only process image links
                if (!link.IsImage)
                    continue;

                var url = link.Url;
                if (string.IsNullOrEmpty(url))
                    continue;

                // Skip external URLs
                if (IsExternalUrl(url))
                    continue;

                // Skip data URLs (embedded images)
                if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if the local file exists
                if (!LocalPathResolver.Exists(
                    analysis,
                    url,
                    baseDirectory,
                    analysis.RootPath,
                    allowDirectory: false,
                    allowMarkdownSibling: false))
                {
                    (var line, var column) = analysis.GetPositionFromOffset(link.Span.Start);

                    yield return CreateViolation(
                        line,
                        column,
                        column + link.Span.Length,
                        $"Image references non-existent file: '{url}'",
                        severity);
                }
            }
        }

        private static bool IsExternalUrl(string url)
        {
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("//", StringComparison.Ordinal);
        }
    }

    internal static class LocalPathResolver
    {
        private static readonly string[] _markdownExtensions = [".md", ".markdown", ".mdown", ".mkd", ".mdx"];

        internal static bool Exists(
            MarkdownDocumentAnalysis analysis,
            string url,
            string baseDirectory,
            string configuredRoot,
            bool allowDirectory,
            bool allowMarkdownSibling)
        {
            try
            {
                string path = GetPathWithoutSuffix(url);
                if (string.IsNullOrEmpty(path))
                    return true;

                path = Uri.UnescapeDataString(path);
                bool isRootRelative = path.StartsWith("/", StringComparison.Ordinal);
                path = path.Replace('/', Path.DirectorySeparatorChar);
                if (isRootRelative)
                {
                    string relativePath = path.TrimStart(Path.DirectorySeparatorChar);
                    if (!string.IsNullOrWhiteSpace(configuredRoot))
                    {
                        string root = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
                            ? configuredRoot
                            : Path.Combine(baseDirectory, configuredRoot));
                        return CandidateExists(
                            analysis,
                            Path.Combine(root, relativePath),
                            allowDirectory,
                            allowMarkdownSibling);
                    }

                    string searchRoot = FindSearchRoot(baseDirectory);
                    DirectoryInfo directory = new(Path.GetFullPath(baseDirectory));
                    while (directory != null && IsPathWithinRoot(directory.FullName, searchRoot))
                    {
                        if (CandidateExists(
                            analysis,
                            Path.Combine(directory.FullName, relativePath),
                            allowDirectory,
                            allowMarkdownSibling))
                        {
                            return true;
                        }

                        directory = directory.Parent;
                    }

                    return false;
                }

                return CandidateExists(
                    analysis,
                    Path.Combine(baseDirectory, path),
                    allowDirectory,
                    allowMarkdownSibling);
            }
            catch (Exception ex) when (
                ex is ArgumentException ||
                ex is IOException ||
                ex is NotSupportedException ||
                ex is UriFormatException ||
                ex is UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static string GetPathWithoutSuffix(string url)
        {
            int queryIndex = url.IndexOf('?');
            int fragmentIndex = url.IndexOf('#');
            int suffixIndex = queryIndex < 0
                ? fragmentIndex
                : fragmentIndex < 0 ? queryIndex : Math.Min(queryIndex, fragmentIndex);

            return suffixIndex < 0 ? url : url.Substring(0, suffixIndex);
        }

        private static bool CandidateExists(
            MarkdownDocumentAnalysis analysis,
            string candidate,
            bool allowDirectory,
            bool allowMarkdownSibling)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (analysis.FileExists(fullPath) || allowDirectory && analysis.DirectoryExists(fullPath))
                return true;

            if (!allowMarkdownSibling ||
                !string.Equals(Path.GetExtension(fullPath), ".html", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string basePath = Path.Combine(
                Path.GetDirectoryName(fullPath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(fullPath) ?? string.Empty);

            return _markdownExtensions.Any(extension => analysis.FileExists(basePath + extension));
        }

        private static string FindSearchRoot(string documentDirectory)
        {
            DirectoryInfo documentFolder = new(Path.GetFullPath(documentDirectory));
            DirectoryInfo directory = documentFolder;

            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                    directory.EnumerateFiles("*.sln").Any() ||
                    directory.EnumerateFiles("*.slnx").Any() ||
                    directory.EnumerateFiles("*.csproj").Any())
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return documentFolder.Parent?.FullName ?? documentFolder.FullName;
        }

        private static bool IsPathWithinRoot(string path, string root)
        {
            string candidate = Path.GetFullPath(path);
            string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            string boundary = normalizedRoot + Path.DirectorySeparatorChar;

            return candidate.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                   candidate.StartsWith(boundary, StringComparison.OrdinalIgnoreCase);
        }
    }
}
