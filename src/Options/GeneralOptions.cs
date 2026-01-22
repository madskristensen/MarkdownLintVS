using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace MarkdownLintVS.Options
{
    /// <summary>
    /// Defines the behavior for running markdown lint fixes on Format Document/Selection.
    /// </summary>
    public enum FormatDocumentBehavior
    {
        /// <summary>
        /// Prompt the user to ask whether to run fixes on formatting commands.
        /// </summary>
        [Description("Ask")]
        Ask,

        /// <summary>
        /// Automatically run markdown lint fixes on Format Document/Selection.
        /// </summary>
        [Description("On")]
        On,

        /// <summary>
        /// Do not run markdown lint fixes on Format Document/Selection.
        /// </summary>
        [Description("Off")]
        Off
    }

    /// <summary>
    /// Defines the behavior for running markdown lint fixes on file save.
    /// </summary>
    public enum FixOnSaveBehavior
    {
        /// <summary>
        /// Prompt the user to ask whether to run fixes on save.
        /// </summary>
        [Description("Ask")]
        Ask,

        /// <summary>
        /// Automatically run markdown lint fixes before saving.
        /// </summary>
        [Description("On")]
        On,

        /// <summary>
        /// Do not run markdown lint fixes on save.
        /// </summary>
        [Description("Off")]
        Off
    }

    /// <summary>
    /// General options for the Markdown Lint extension.
    /// </summary>
    public class GeneralOptions : BaseOptionModel<GeneralOptions>, IRatingConfig
    {
        [Category("General")]
        [DisplayName("Linting Enabled")]
        [Description("Controls whether markdown linting is enabled.")]
        [DefaultValue(true)]
        public bool LintingEnabled { get; set; } = true;

        [Category("Formatting")]
        [DisplayName("Format Document Behavior")]
        [Description("Controls whether markdown lint fixes are automatically applied when using Format Document or Format Selection commands.")]
        [DefaultValue(FormatDocumentBehavior.Ask)]
        public FormatDocumentBehavior FormatDocumentBehavior { get; set; } = FormatDocumentBehavior.Ask;

        [Category("Formatting")]
        [DisplayName("Fix on Save Behavior")]
        [Description("Controls whether markdown lint fixes are automatically applied before saving a Markdown file.")]
        [DefaultValue(FixOnSaveBehavior.Ask)]
        public FixOnSaveBehavior FixOnSaveBehavior { get; set; } = FixOnSaveBehavior.Ask;

        [Category("Folder Linting")]
        [DisplayName("Ignored Folders")]
        [Description("Comma-separated list of folder names to always ignore when linting a folder. These are ignored in addition to patterns in .markdownlintignore files.")]
        [DefaultValue("node_modules, vendor, .git, bin, obj, packages, TestResults")]
        public string IgnoredFolders { get; set; } = "node_modules, vendor, .git, bin, obj, packages, TestResults";

        /// <summary>
        /// Gets the ignored folder names as an array.
        /// </summary>
        public string[] GetIgnoredFolderNames()
        {
            if (string.IsNullOrWhiteSpace(IgnoredFolders))
                return [];

            return [.. IgnoredFolders
                .Split(',')
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrEmpty(f))];
        }

        // IRatingConfig implementation

        /// <inheritdoc/>
        [Browsable(false)]
        public int RatingRequests { get; set; }

        /// <inheritdoc/>
        Task IRatingConfig.SaveAsync() => SaveAsync();
    }

    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptionsPage : BaseOptionPage<GeneralOptions> { }
    }
}

