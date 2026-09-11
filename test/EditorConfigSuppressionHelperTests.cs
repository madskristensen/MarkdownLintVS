using System.IO;
using MarkdownLintVS.Commands;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class EditorConfigSuppressionHelperTests
{
    [TestMethod]
    public void WhenExistingEditorConfigHasApplicableSectionThenRuleIsAddedThere()
    {
        using var fixture = new EditorConfigFixture("[*.md]\r\nindent_size = 4\r\n\r\n[*.cs]\r\nindent_size = 4\r\n");

        var changed = EditorConfigSuppressionHelper.Suppress(fixture.MarkdownPath, "no-trailing-spaces");

        Assert.IsTrue(changed);
        Assert.AreEqual("[*.md]\r\nindent_size = 4\r\nmd_no_trailing_spaces = false\r\n\r\n[*.cs]\r\nindent_size = 4\r\n", File.ReadAllText(fixture.EditorConfigPath));
    }

    [TestMethod]
    public void WhenExistingRuleIsConfiguredThenValueIsUpdated()
    {
        using var fixture = new EditorConfigFixture("[*.md]\nmd_no_trailing_spaces = warning\nother = true\n");

        EditorConfigSuppressionHelper.Suppress(fixture.MarkdownPath, "no-trailing-spaces");

        StringAssert.Contains(File.ReadAllText(fixture.EditorConfigPath), "md_no_trailing_spaces = false");
        Assert.DoesNotContain("warning", File.ReadAllText(fixture.EditorConfigPath));
    }

    [TestMethod]
    public void WhenNoEditorConfigExistsInRepositoryThenRepositoryRootIsUsed()
    {
        using var fixture = new EditorConfigFixture(null, createRepository: true);

        EditorConfigSuppressionHelper.Suppress(fixture.MarkdownPath, "no-hard-tabs");

        Assert.IsTrue(File.Exists(fixture.EditorConfigPath));
        StringAssert.Contains(File.ReadAllText(fixture.EditorConfigPath), "md_no_hard_tabs = false");
    }

    [TestMethod]
    public void WhenNoEditorConfigOrRepositoryExistsThenSolutionRootIsUsed()
    {
        using var fixture = new EditorConfigFixture(null);
        var solutionRoot = Directory.CreateDirectory(Path.Combine(fixture.RootPath, "solution")).FullName;

        EditorConfigSuppressionHelper.Suppress(fixture.MarkdownPath, "line-length", solutionRoot);

        var solutionEditorConfig = Path.Combine(solutionRoot, ".editorconfig");
        Assert.IsTrue(File.Exists(solutionEditorConfig));
        StringAssert.Contains(File.ReadAllText(solutionEditorConfig), "md_line_length = false");
    }

    [TestMethod]
    public void WhenNoEditorConfigRepositoryOrSolutionExistsThenNoTargetIsReturned()
    {
        using var fixture = new EditorConfigFixture(null);

        Assert.IsNull(EditorConfigSuppressionHelper.FindTargetPath(fixture.MarkdownPath));
        Assert.IsFalse(EditorConfigSuppressionHelper.Suppress(fixture.MarkdownPath, "line-length"));
    }

    private sealed class EditorConfigFixture : IDisposable
    {
        public string RootPath { get; }
        public string MarkdownPath { get; }
        public string EditorConfigPath => Path.Combine(RootPath, ".editorconfig");

        public EditorConfigFixture(string? editorConfig, bool createRepository = false)
        {
            RootPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "MarkdownLintTests", Guid.NewGuid().ToString("N"))).FullName;
            MarkdownPath = Path.Combine(RootPath, "docs", "test.md");
            Directory.CreateDirectory(Path.GetDirectoryName(MarkdownPath));
            File.WriteAllText(MarkdownPath, "# Test\n");
            if (editorConfig != null)
                File.WriteAllText(EditorConfigPath, editorConfig);
            if (createRepository)
                Directory.CreateDirectory(Path.Combine(RootPath, ".git"));
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
    }
}
