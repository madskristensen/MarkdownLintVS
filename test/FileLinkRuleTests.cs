using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;
using System.IO;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class FileLinkRuleTests
{
    private static RuleConfiguration DefaultConfig => new();

    private static string TestDirectory => Path.GetDirectoryName(typeof(FileLinkRuleTests).Assembly.Location);

    #region MD061 - File Links Exist

    [TestMethod]
    public void MD061_WhenNoFilePathThenNoViolations()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[link](nonexistent.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD061_WhenExternalLinkThenNoViolations()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[link](https://example.com)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD061_WhenMailtoLinkThenNoViolations()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[email](mailto:test@example.com)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD061_WhenFragmentOnlyThenNoViolations()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[section](#heading)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD061_WhenLocalFileMissingThenReportsViolation()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[link](nonexistent-file-12345.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD061", violations[0].Rule.Id);
        Assert.Contains("nonexistent-file-12345.md", violations[0].Message);
    }

    [TestMethod]
    public void MD061_WhenImageLinkThenNoViolations()
    {
        // MD061 should skip image links (those are handled by MD062)
        var rule = new MD061_FileLinkExists();
        var markdown = "![alt](nonexistent-image.png)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD061_WhenDataUrlThenNoViolations()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[link](data:text/plain;base64,SGVsbG8=)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD061_WhenProtocolRelativeUrlThenNoViolations()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[link](//example.com/page)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD061_WhenLinkWithFragmentAndFileMissingThenReportsViolation()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[link](missing-file.md#section)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.Contains("missing-file.md", violations[0].Message);
    }

    #endregion

    #region MD062 - Image Links Exist

    [TestMethod]
    public void MD062_WhenNoFilePathThenNoViolations()
    {
        var rule = new MD062_ImageLinkExists();
        var markdown = "![alt](nonexistent.png)";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD062_WhenExternalImageThenNoViolations()
    {
        var rule = new MD062_ImageLinkExists();
        var markdown = "![alt](https://example.com/image.png)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD062_WhenLocalImageMissingThenReportsViolation()
    {
        var rule = new MD062_ImageLinkExists();
        var markdown = "![alt](nonexistent-image-12345.png)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.HasCount(1, violations);
        Assert.AreEqual("MD062", violations[0].Rule.Id);
        Assert.Contains("nonexistent-image-12345.png", violations[0].Message);
    }

    [TestMethod]
    public void MD062_WhenRegularLinkThenNoViolations()
    {
        // MD062 should only check image links
        var rule = new MD062_ImageLinkExists();
        var markdown = "[link](nonexistent.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD062_WhenDataUrlImageThenNoViolations()
    {
        var rule = new MD062_ImageLinkExists();
        var markdown = "![alt](data:image/png;base64,iVBORw0KGgo=)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    [TestMethod]
    public void MD062_WhenProtocolRelativeImageThenNoViolations()
    {
        var rule = new MD062_ImageLinkExists();
        var markdown = "![alt](//example.com/image.png)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        Assert.IsEmpty(violations);
    }

    #endregion

    #region Root Path Resolution

    [TestMethod]
    public void MD061_WhenRootRelativePathWithNoRootConfiguredThenReportsViolation()
    {
        var rule = new MD061_FileLinkExists();
        var markdown = "[link](/docs/missing.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Without root_path configured, root-relative paths fall back to base directory
        Assert.HasCount(1, violations);
    }

    [TestMethod]
    public void MD061_RootPathFromFrontMatterTakesPrecedence()
    {
        // Test that front matter root_path is used
        var markdown =
            "---\n" +
            "root_path: C:\\TestRoot\n" +
            "---\n" +
            "\n" +
            "[link](/docs/file.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        Assert.AreEqual(@"C:\TestRoot", analysis.FrontMatterRootPath);
        Assert.AreEqual(@"C:\TestRoot", analysis.RootPath);
    }

    [TestMethod]
    public void MD061_EditorConfigRootPathUsedWhenNoFrontMatter()
    {
        var markdown = "[link](/docs/file.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));
        analysis.EditorConfigRootPath = @"C:\EditorConfigRoot";

        Assert.IsNull(analysis.FrontMatterRootPath);
        Assert.AreEqual(@"C:\EditorConfigRoot", analysis.RootPath);
    }

    [TestMethod]
    public void MD061_OptionsRootPathUsedWhenNoOtherConfig()
    {
        var markdown = "[link](/docs/file.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));
        analysis.OptionsRootPath = @"C:\OptionsRoot";

        Assert.IsNull(analysis.FrontMatterRootPath);
        Assert.IsNull(analysis.EditorConfigRootPath);
        Assert.AreEqual(@"C:\OptionsRoot", analysis.RootPath);
    }

    [TestMethod]
    public void MD061_RootPathPrecedence_FrontMatterOverEditorConfig()
    {
        var markdown =
            "---\n" +
            "root_path: C:\\FrontMatterRoot\n" +
            "---\n";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));
        analysis.EditorConfigRootPath = @"C:\EditorConfigRoot";
        analysis.OptionsRootPath = @"C:\OptionsRoot";

        Assert.AreEqual(@"C:\FrontMatterRoot", analysis.RootPath);
    }

    [TestMethod]
    public void MD061_RootPathPrecedence_EditorConfigOverOptions()
    {
        var markdown = "[link](/docs/file.md)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));
        analysis.EditorConfigRootPath = @"C:\EditorConfigRoot";
        analysis.OptionsRootPath = @"C:\OptionsRoot";

        Assert.AreEqual(@"C:\EditorConfigRoot", analysis.RootPath);
    }

    [TestMethod]
    public void MD061_FrontMatterRootPathWithQuotes()
    {
        var markdown =
            "---\n" +
            "root_path: \"C:\\Quoted\\Path\"\n" +
            "---\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.AreEqual(@"C:\Quoted\Path", analysis.FrontMatterRootPath);
    }

    [TestMethod]
    public void MD061_FrontMatterRootPathWithSingleQuotes()
    {
        var markdown =
            "---\n" +
            "root_path: 'C:\\SingleQuoted\\Path'\n" +
            "---\n";
        var analysis = new MarkdownDocumentAnalysis(markdown);

        Assert.AreEqual(@"C:\SingleQuoted\Path", analysis.FrontMatterRootPath);
    }

    [TestMethod]
    public void MD062_WhenRootRelativeImageWithNoRootConfiguredThenReportsViolation()
    {
        var rule = new MD062_ImageLinkExists();
        var markdown = "![alt](/images/missing.png)";
        var analysis = new MarkdownDocumentAnalysis(markdown, Path.Combine(TestDirectory, "test.md"));

        var violations = rule.Analyze(analysis, DefaultConfig, DiagnosticSeverity.Warning).ToList();

        // Without root_path configured, root-relative paths fall back to base directory
        Assert.HasCount(1, violations);
    }

    #endregion
}
