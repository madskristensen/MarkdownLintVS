using MarkdownLintVS.Tagging;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class DocumentPathTrackerTests
{
    [TestMethod]
    public void WhenPathChangesThenOldPathIsReturnedAndCurrentPathIsUpdated()
    {
        var tracker = new DocumentPathTracker(@"C:\docs\old.md");

        string oldPath = tracker.Update(@"C:\docs\new.md");

        Assert.AreEqual(@"C:\docs\old.md", oldPath);
        Assert.AreEqual(@"C:\docs\new.md", tracker.CurrentPath);
    }

    [TestMethod]
    public void WhenOnlyPathCasingChangesThenNoChangeIsReported()
    {
        var tracker = new DocumentPathTracker(@"C:\docs\readme.md");

        string oldPath = tracker.Update(@"C:\DOCS\README.md");

        Assert.IsNull(oldPath);
        Assert.AreEqual(@"C:\docs\readme.md", tracker.CurrentPath);
    }
}
