using MarkdownLintVS.ErrorList;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownDocumentListenerTests
{
    [TestMethod]
    public void WhenOneOfTwoViewsClosesThenDocumentRemainsActive()
    {
        var references = new ViewReferenceCounter();
        references.Add();
        references.Add();

        bool shouldDispose = references.Remove();

        Assert.IsFalse(shouldDispose);
        Assert.AreEqual(1, references.Count);
    }

    [TestMethod]
    public void WhenLastViewClosesThenDocumentIsReleased()
    {
        var references = new ViewReferenceCounter();
        references.Add();

        bool shouldDispose = references.Remove();

        Assert.IsTrue(shouldDispose);
        Assert.AreEqual(0, references.Count);
    }

    [TestMethod]
    public void WhenViewIsRemovedTwiceThenDocumentIsNotReleasedAgain()
    {
        var references = new ViewReferenceCounter();
        references.Add();
        _ = references.Remove();

        bool shouldDispose = references.Remove();

        Assert.IsFalse(shouldDispose);
        Assert.AreEqual(0, references.Count);
    }
}
