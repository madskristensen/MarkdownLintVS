using System.IO;
using System.Linq;
using System.Threading;
using MarkdownLintVS.Linting;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownFileScannerTests
{
    [TestMethod]
    public void WhenIgnorePatternThenNegationPatternThenMatchingFileIsIncluded()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, ".markdownlintignore"), "*.md\n!keep.md");
        File.WriteAllText(Path.Combine(root, "keep.md"), "# keep");
        File.WriteAllText(Path.Combine(root, "drop.md"), "# drop");

        var scanner = new MarkdownFileScanner(root);

        var results = scanner.ScanForMarkdownFiles();

        Assert.HasCount(1, results);
        Assert.IsTrue(results.Any(path => path.EndsWith("keep.md", System.StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void WhenNegationPatternThenIgnorePatternThenLastRuleWinsAndFileIsIgnored()
    {
        var root = CreateTempRoot();
        File.WriteAllText(Path.Combine(root, ".markdownlintignore"), "!keep.md\n*.md");
        File.WriteAllText(Path.Combine(root, "keep.md"), "# keep");

        var scanner = new MarkdownFileScanner(root);

        var results = scanner.ScanForMarkdownFiles();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void WhenNestedIgnoreFileThenPatternsAreRelativeAndOverrideParentRules()
    {
        var root = CreateTempRoot();
        var nested = Directory.CreateDirectory(Path.Combine(root, "docs")).FullName;
        var sibling = Directory.CreateDirectory(Path.Combine(root, "other")).FullName;
        File.WriteAllText(Path.Combine(root, ".markdownlintignore"), "*.md");
        File.WriteAllText(Path.Combine(nested, ".markdownlintignore"), "!keep.md");
        File.WriteAllText(Path.Combine(nested, "keep.md"), "# keep");
        File.WriteAllText(Path.Combine(nested, "drop.md"), "# drop");
        File.WriteAllText(Path.Combine(sibling, "keep.md"), "# sibling");

        var results = new MarkdownFileScanner(root).ScanForMarkdownFiles();

        Assert.HasCount(1, results);
        Assert.AreEqual(Path.Combine(nested, "keep.md"), results[0]);
    }

    [TestMethod]
    public async Task AsyncScan_WhenAlreadyCancelledThenThrows()
    {
        var scanner = new MarkdownFileScanner(CreateTempRoot());
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(
            () => scanner.ScanForMarkdownFilesAsync(source.Token));
    }

    [TestMethod]
    public void ReparsePointDirectoriesAreNotTraversed()
    {
        Assert.IsFalse(MarkdownFileScanner.ShouldTraverseDirectory(FileAttributes.Directory | FileAttributes.ReparsePoint));
        Assert.IsTrue(MarkdownFileScanner.ShouldTraverseDirectory(FileAttributes.Directory));
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "MarkdownLintVS.Tests", System.Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(root);
        return root;
    }
}
