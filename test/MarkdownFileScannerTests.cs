using System.Diagnostics;
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
        using var temp = new TempDirectory();
        File.WriteAllText(temp.File(".markdownlintignore"), "*.md\n!keep.md");
        File.WriteAllText(temp.File("keep.md"), "# keep");
        File.WriteAllText(temp.File("drop.md"), "# drop");

        var scanner = new MarkdownFileScanner(temp.Path);

        var results = scanner.ScanForMarkdownFiles();

        Assert.HasCount(1, results);
        Assert.IsTrue(results.Any(path => path.EndsWith("keep.md", System.StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void WhenNegationPatternThenIgnorePatternThenLastRuleWinsAndFileIsIgnored()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(temp.File(".markdownlintignore"), "!keep.md\n*.md");
        File.WriteAllText(temp.File("keep.md"), "# keep");

        var scanner = new MarkdownFileScanner(temp.Path);

        var results = scanner.ScanForMarkdownFiles();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void WhenNestedIgnoreFileThenPatternsAreRelativeAndOverrideParentRules()
    {
        using var temp = new TempDirectory();
        var nested = Directory.CreateDirectory(temp.File("docs")).FullName;
        var sibling = Directory.CreateDirectory(temp.File("other")).FullName;
        File.WriteAllText(temp.File(".markdownlintignore"), "*.md");
        File.WriteAllText(Path.Combine(nested, ".markdownlintignore"), "!keep.md");
        File.WriteAllText(Path.Combine(nested, "keep.md"), "# keep");
        File.WriteAllText(Path.Combine(nested, "drop.md"), "# drop");
        File.WriteAllText(Path.Combine(sibling, "keep.md"), "# sibling");

        var results = new MarkdownFileScanner(temp.Path).ScanForMarkdownFiles();

        Assert.HasCount(1, results);
        Assert.AreEqual(Path.Combine(nested, "keep.md"), results[0]);
    }

    [TestMethod]
    public async Task AsyncScan_WhenAlreadyCancelledThenThrows()
    {
        using var temp = new TempDirectory();
        var scanner = new MarkdownFileScanner(temp.Path);
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

    [TestMethod]
    public void DirectoryJunctionsAreNotTraversed()
    {
        using var temp = new TempDirectory();
        using var target = new TempDirectory();
        string junction = temp.File("linked");
        File.WriteAllText(temp.File("root.md"), "# root");
        File.WriteAllText(target.File("linked.md"), "# linked");
        CreateJunction(junction, target.Path);

        try
        {
            IReadOnlyList<string> results = new MarkdownFileScanner(temp.Path).ScanForMarkdownFiles();

            CollectionAssert.AreEqual(new[] { temp.File("root.md") }, results.ToArray());
        }
        finally
        {
            if (Directory.Exists(junction))
                Directory.Delete(junction);
        }
    }

    [TestMethod]
    public void RootAnchoredPatternDoesNotIgnoreNestedFile()
    {
        using var temp = new TempDirectory();
        string nested = Directory.CreateDirectory(temp.File("docs")).FullName;
        File.WriteAllText(temp.File(".markdownlintignore"), "/ignored.md");
        File.WriteAllText(temp.File("ignored.md"), "# root");
        File.WriteAllText(Path.Combine(nested, "ignored.md"), "# nested");

        IReadOnlyList<string> results = new MarkdownFileScanner(temp.Path).ScanForMarkdownFiles();

        CollectionAssert.AreEqual(new[] { Path.Combine(nested, "ignored.md") }, results.ToArray());
    }

    [TestMethod]
    public void NestedAnchoredPatternIsRelativeToItsIgnoreFile()
    {
        using var temp = new TempDirectory();
        string docs = Directory.CreateDirectory(temp.File("docs")).FullName;
        string deeper = Directory.CreateDirectory(Path.Combine(docs, "nested")).FullName;
        File.WriteAllText(Path.Combine(docs, ".markdownlintignore"), "/ignored.md");
        File.WriteAllText(Path.Combine(docs, "ignored.md"), "# ignored");
        File.WriteAllText(Path.Combine(deeper, "ignored.md"), "# included");

        IReadOnlyList<string> results = new MarkdownFileScanner(temp.Path).ScanForMarkdownFiles();

        CollectionAssert.AreEqual(new[] { Path.Combine(deeper, "ignored.md") }, results.ToArray());
    }

    [TestMethod]
    public void CancellationDuringTraversalStopsBeforeReadingNextDirectory()
    {
        using var temp = new TempDirectory();
        string nested = Directory.CreateDirectory(temp.File("docs")).FullName;
        File.WriteAllText(Path.Combine(nested, "file.md"), "# file");
        using var source = new CancellationTokenSource();
        var scanner = new MarkdownFileScanner(temp.Path, directory =>
        {
            if (directory == nested)
                source.Cancel();
        });

        Assert.ThrowsExactly<OperationCanceledException>(
            () => scanner.ScanForMarkdownFiles(source.Token));
    }

    [TestMethod]
    public void DirectoryThatDisappearsDuringTraversalIsSkipped()
    {
        using var temp = new TempDirectory();
        string nested = Directory.CreateDirectory(temp.File("docs")).FullName;
        File.WriteAllText(temp.File("root.md"), "# root");
        var scanner = new MarkdownFileScanner(temp.Path, directory =>
        {
            if (directory == nested)
                Directory.Delete(nested, recursive: true);
        });

        IReadOnlyList<string> results = scanner.ScanForMarkdownFiles();

        CollectionAssert.AreEqual(new[] { temp.File("root.md") }, results.ToArray());
    }

    private static void CreateJunction(string junction, string target)
    {
        string commandInterpreter = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
        var startInfo = new ProcessStartInfo(
            commandInterpreter,
            $"/c mklink /J \"{junction}\" \"{target}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start the command interpreter.");
        process.WaitForExit();
        Assert.AreEqual(
            0,
            process.ExitCode,
            $"Unable to create test junction: {process.StandardError.ReadToEnd()}");
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MarkdownLintVS.Tests",
                Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string File(string relativePath) => System.IO.Path.Combine(Path, relativePath);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
