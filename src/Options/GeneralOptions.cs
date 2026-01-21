using System.ComponentModel;
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
    /// General options for the Markdown Lint extension.
    /// </summary>
    public class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        [Category("Formatting")]
        [DisplayName("Format Document Behavior")]
        [Description("Controls whether markdown lint fixes are automatically applied when using Format Document or Format Selection commands.")]
        [DefaultValue(FormatDocumentBehavior.Ask)]
        public FormatDocumentBehavior FormatDocumentBehavior { get; set; } = FormatDocumentBehavior.Ask;
    }

    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptionsPage : BaseOptionPage<GeneralOptions> { }
    }
}
