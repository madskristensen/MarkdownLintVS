using MarkdownLintVS.ErrorList;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownLintTableDataSourceTests
{
    [TestMethod]
    public void WhenFolderLintFileHasLiveSnapshotThenFolderErrorIsExcluded()
    {
        var liveFiles = new[]
        {
            @"C:\repo\docs\open.md",
            @"C:\repo\docs\other.md"
        };

        bool include = ErrorListDeduplication.ShouldIncludeFolderLintError(
            @"C:\REPO\docs\OPEN.md",
            liveFiles);

        Assert.IsFalse(include);
    }

    [TestMethod]
    public void WhenFolderLintFileHasNoLiveSnapshotThenFolderErrorIsIncluded()
    {
        var liveFiles = new[]
        {
            @"C:\repo\docs\open.md"
        };

        bool include = ErrorListDeduplication.ShouldIncludeFolderLintError(
            @"C:\repo\docs\closed.md",
            liveFiles);

        Assert.IsTrue(include);
    }
}
