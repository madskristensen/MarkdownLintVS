using MarkdownLintVS.Linting;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class MarkdownAnalysisCacheTests
{
    [TestMethod]
    public void WhenCachedAnalysisMatchesSnapshotThenViolationsAreReturned()
    {
        var violations = new List<LintViolation>();
        var cached = new CachedAnalysisResult(7, violations);

        IReadOnlyList<LintViolation> result = MarkdownAnalysisCache.GetViolationsForSnapshot(cached, 7);

        Assert.AreSame(violations, result);
    }

    [TestMethod]
    public void WhenCachedAnalysisIsStaleThenNoViolationsAreReturned()
    {
        var cached = new CachedAnalysisResult(7, new List<LintViolation>());

        IReadOnlyList<LintViolation> result = MarkdownAnalysisCache.GetViolationsForSnapshot(cached, 8);

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void PendingAnalysisMatchesOnlyItsSnapshot()
    {
        using var source = new CancellationTokenSource();
        var pending = new PendingAnalysis(source, 7, isDebounced: true);

        Assert.IsTrue(MarkdownAnalysisCache.IsPendingAnalysisForSnapshot(pending, 7));
        Assert.IsFalse(MarkdownAnalysisCache.IsPendingAnalysisForSnapshot(pending, 8));
    }

    [TestMethod]
    public void ImmediateAnalysisDoesNotReuseDebouncedWork()
    {
        using var source = new CancellationTokenSource();
        var debounced = new PendingAnalysis(source, 7, isDebounced: true);

        Assert.IsFalse(MarkdownAnalysisCache.IsPendingImmediateAnalysisForSnapshot(debounced, 7));
    }

    [TestMethod]
    public void ImmediateAnalysisReusesMatchingImmediateWork()
    {
        using var source = new CancellationTokenSource();
        var immediate = new PendingAnalysis(source, 7, isDebounced: false);

        Assert.IsTrue(MarkdownAnalysisCache.IsPendingImmediateAnalysisForSnapshot(immediate, 7));
    }

    [TestMethod]
    public void CompletedAnalysisPublishesOnlyForCurrentOwnedSnapshot()
    {
        using var source = new CancellationTokenSource();
        using var replacementSource = new CancellationTokenSource();
        var completed = new PendingAnalysis(source, 7, isDebounced: false);
        var replacement = new PendingAnalysis(replacementSource, 7, isDebounced: false);

        Assert.IsTrue(MarkdownAnalysisCache.CanPublishAnalysis(false, 7, 7, completed, completed));
        Assert.IsFalse(MarkdownAnalysisCache.CanPublishAnalysis(true, 7, 7, completed, completed));
        Assert.IsFalse(MarkdownAnalysisCache.CanPublishAnalysis(false, 7, 8, completed, completed));
        Assert.IsFalse(MarkdownAnalysisCache.CanPublishAnalysis(false, 7, 7, completed, replacement));
    }
}
