using System.IO;
using System.Linq;
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

        Assert.AreEqual(1, results.Count);
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

        Assert.AreEqual(0, results.Count);
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "MarkdownLintVS.Tests", System.Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(root);
        return root;
    }
}
