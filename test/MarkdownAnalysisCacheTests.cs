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
}
