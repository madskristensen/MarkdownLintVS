using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarkdownLintVS.ErrorList;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;
using MarkdownLintVS.Options;

namespace MarkdownLintVS.Commands
{
    /// <summary>
    /// Command to lint all Markdown files in a folder from Solution Explorer.
    /// </summary>
    [Command(PackageIds.LintFolderCommand)]
    internal sealed class LintFolderCommand : BaseCommand<LintFolderCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                var selectedPath = await GetSelectedFolderPathAsync();
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    await LintFolderAsync(selectedPath);
                }
                else
                {
                    await VS.MessageBox.ShowWarningAsync("Markdown Lint", "Could not determine the folder path. Please select a folder in Solution Explorer.");
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Markdown Lint", $"Error linting folder: {ex.Message}");
            }
        }

        private static async Task<string> GetSelectedFolderPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IEnumerable<SolutionItem> selectedItems = await VS.Solutions.GetActiveItemsAsync();
            SolutionItem firstItem = selectedItems?.FirstOrDefault();

            if (firstItem != null)
            {
                // For folders, get the full path
                // Check multiple folder types that might be used in Solution Explorer
                if (firstItem.Type == SolutionItemType.PhysicalFolder ||
                    firstItem.Type == SolutionItemType.VirtualFolder ||
                    firstItem.Type == SolutionItemType.SolutionFolder)
                {
                    return firstItem.FullPath;
                }

                // If it's a physical file or other item, try to get its directory
                if (!string.IsNullOrEmpty(firstItem.FullPath) && Directory.Exists(firstItem.FullPath))
                {
                    return firstItem.FullPath;
                }
            }

            return null;
        }

        internal static async Task LintFolderAsync(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                await VS.MessageBox.ShowWarningAsync("Markdown Lint", $"Folder not found: {folderPath}");
                return;
            }

            await VS.StatusBar.ShowMessageAsync($"Scanning for Markdown files in {Path.GetFileName(folderPath)}...");

            try
            {
                // Scan for files
                var scanner = new MarkdownFileScanner(folderPath);
                IReadOnlyList<string> files = scanner.ScanForMarkdownFiles();

                if (files.Count == 0)
                {
                    await VS.StatusBar.ShowMessageAsync("No Markdown files found.");
                    await VS.MessageBox.ShowAsync("Markdown Lint", "No Markdown files found in the selected folder.");
                    return;
                }

                // Lint files in parallel with progress
                List<(string FilePath, LintViolation Violation)> allViolations = await LintFilesInParallelAsync(files);

                // Report results
                await ReportResultsAsync(allViolations, files.Count);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowMessageAsync("Markdown lint failed.");
                await VS.MessageBox.ShowErrorAsync("Markdown Lint", $"Error linting folder: {ex.Message}");
            }
        }

        private static async Task<List<(string FilePath, LintViolation Violation)>> LintFilesInParallelAsync(
            IReadOnlyList<string> files)
        {
            var results = new ConcurrentBag<(string FilePath, LintViolation Violation)>();
            var processedCount = 0;
            var totalCount = files.Count;

            // Get rule configurations
            Dictionary<string, RuleConfiguration> ruleConfigs = RuleOptionsProvider.Instance.GetRuleConfigurations();

            // Use parallel processing with progress reporting
            await Task.Run(() =>
            {
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    filePath =>
                    {
                        try
                        {
                            var text = File.ReadAllText(filePath);
                            var analysis = new MarkdownDocumentAnalysis(text);

                            // Get EditorConfig settings for this file's directory
                            var fileDir = Path.GetDirectoryName(filePath);
                            Dictionary<string, RuleConfiguration> editorConfigSettings = MarkdownLintAnalyzer.GetEditorConfigSettings(fileDir);

                            IEnumerable<LintViolation> violations = MarkdownLintAnalyzer.Analyze(analysis, ruleConfigs, editorConfigSettings);

                            foreach (LintViolation violation in violations)
                            {
                                results.Add((filePath, violation));
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log file-level errors but continue processing
                            System.Diagnostics.Debug.WriteLine($"Error linting {filePath}: {ex.Message}");
                        }

                        // Update progress
                        var current = Interlocked.Increment(ref processedCount);
                        if (current % 10 == 0 || current == totalCount)
                        {
                            ThreadHelper.JoinableTaskFactory.Run(async () =>
                            {
                                await VS.StatusBar.ShowMessageAsync($"Linting Markdown files... ({current}/{totalCount})");
                            });
                        }
                    });
            });

            return [.. results];
        }

        private static async Task ReportResultsAsync(
            List<(string FilePath, LintViolation Violation)> violations,
            int totalFiles)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Ensure the data source is initialized (may not be if no markdown file is open)
            MarkdownLintTableDataSource dataSource = await MarkdownLintTableDataSource.EnsureInitializedAsync();
            if (dataSource == null)
            {
                // Data source could not be initialized - just show message
                await VS.StatusBar.ShowMessageAsync(
                    violations.Count > 0
                        ? $"Markdown Lint: {violations.Count} issues found ({totalFiles} files scanned)"
                        : $"Markdown Lint: No issues found ({totalFiles} files scanned)");
                return;
            }

            // Clear previous folder lint results and add new ones
            dataSource.ClearFolderLintErrors();

            foreach ((var filePath, LintViolation violation) in violations)
            {
                dataSource.AddFolderLintError(
                    filePath,
                    violation.LineNumber,
                    violation.ColumnStart,
                    violation.Rule.Id,
                    violation.Message,
                    violation.Severity);
            }

            // Show summary in status bar
            var fileCount = violations.Select(v => v.FilePath).Distinct().Count();
            var issueText = violations.Count == 1 ? "issue" : "issues";
            var fileText = fileCount == 1 ? "file" : "files";

            if (violations.Count > 0)
            {
                await VS.StatusBar.ShowMessageAsync(
                    $"Markdown Lint: {violations.Count} {issueText} in {fileCount} {fileText} ({totalFiles} files scanned)");

                // Show Error List
                await VS.Commands.ExecuteAsync("View.ErrorList");
            }
            else
            {
                await VS.StatusBar.ShowMessageAsync(
                    $"Markdown Lint: No issues found ({totalFiles} files scanned)");
            }
        }
    }

    /// <summary>
    /// Command to lint all Markdown files in a project from Solution Explorer.
    /// </summary>
    [Command(PackageIds.LintProjectCommand)]
    internal sealed class LintProjectCommand : BaseCommand<LintProjectCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                var selectedPath = await GetSelectedProjectPathAsync();
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    await LintFolderCommand.LintFolderAsync(selectedPath);
                }
                else
                {
                    await VS.MessageBox.ShowWarningAsync("Markdown Lint", "Could not determine the project path. Please select a project in Solution Explorer.");
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Markdown Lint", $"Error linting project: {ex.Message}");
            }
        }

        private static async Task<string> GetSelectedProjectPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IEnumerable<SolutionItem> selectedItems = await VS.Solutions.GetActiveItemsAsync();
            SolutionItem firstItem = selectedItems?.FirstOrDefault();

            if (firstItem != null && firstItem.Type == SolutionItemType.Project)
            {
                // Get the project directory
                var projectPath = firstItem.FullPath;
                if (!string.IsNullOrEmpty(projectPath))
                {
                    return Path.GetDirectoryName(projectPath);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Command to lint all Markdown files in a solution from Solution Explorer.
    /// </summary>
    [Command(PackageIds.LintSolutionCommand)]
    internal sealed class LintSolutionCommand : BaseCommand<LintSolutionCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                var solutionPath = await GetSolutionDirectoryAsync();
                if (!string.IsNullOrEmpty(solutionPath))
                {
                    await LintFolderCommand.LintFolderAsync(solutionPath);
                }
                else
                {
                    await VS.MessageBox.ShowWarningAsync("Markdown Lint", "Could not determine the solution path. Please ensure a solution is open.");
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowErrorAsync("Markdown Lint", $"Error linting solution: {ex.Message}");
            }
        }

        private static async Task<string> GetSolutionDirectoryAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Solution solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution != null && !string.IsNullOrEmpty(solution.FullPath))
            {
                return Path.GetDirectoryName(solution.FullPath);
            }

            // For Open Folder scenarios, try to get workspace folder
            // VS.Solutions may return null, so fall back to active items
            IEnumerable<SolutionItem> selectedItems = await VS.Solutions.GetActiveItemsAsync();
            SolutionItem firstItem = selectedItems?.FirstOrDefault();

            if (firstItem != null && firstItem.Type == SolutionItemType.Solution)
            {
                return Path.GetDirectoryName(firstItem.FullPath);
            }

            return null;
        }
    }
}
