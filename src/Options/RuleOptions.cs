using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MarkdownLintVS.Options
{
    /// <summary>
    /// Options for all markdown lint rules.
    /// These settings are used as defaults when no .editorconfig rule is present.
    /// </summary>
    public class RuleOptions : BaseOptionModel<RuleOptions>
    {
        // ===== Heading Rules =====

        [Category("1. Headings")]
        [DisplayName("MD001 - heading-increment")]
        [Description("Heading levels should only increment by one level at a time")]
        [DefaultValue(true)]
        public bool MD001 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD003 - heading-style")]
        [Description("Heading style should be consistent")]
        [DefaultValue(true)]
        public bool MD003 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD018 - no-missing-space-atx")]
        [Description("No space after hash on atx style heading")]
        [DefaultValue(true)]
        public bool MD018 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD019 - no-multiple-space-atx")]
        [Description("Multiple spaces after hash on atx style heading")]
        [DefaultValue(true)]
        public bool MD019 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD020 - no-missing-space-closed-atx")]
        [Description("No space inside hashes on closed atx style heading")]
        [DefaultValue(true)]
        public bool MD020 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD021 - no-multiple-space-closed-atx")]
        [Description("Multiple spaces inside hashes on closed atx style heading")]
        [DefaultValue(true)]
        public bool MD021 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD022 - blanks-around-headings")]
        [Description("Headings should be surrounded by blank lines")]
        [DefaultValue(true)]
        public bool MD022 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD023 - heading-start-left")]
        [Description("Headings must start at the beginning of the line")]
        [DefaultValue(true)]
        public bool MD023 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD024 - no-duplicate-heading")]
        [Description("Multiple headings with the same content")]
        [DefaultValue(true)]
        public bool MD024 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD025 - single-title")]
        [Description("Multiple top-level headings in the same document")]
        [DefaultValue(true)]
        public bool MD025 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD026 - no-trailing-punctuation")]
        [Description("Trailing punctuation in heading")]
        [DefaultValue(true)]
        public bool MD026 { get; set; } = true;

        [Category("1. Headings")]
        [DisplayName("MD041 - first-line-heading")]
        [Description("First line in a file should be a top-level heading")]
        [DefaultValue(false)]
        public bool MD041 { get; set; } = false;

        // ===== List Rules =====

        [Category("2. Lists")]
        [DisplayName("MD004 - ul-style")]
        [Description("Unordered list style should be consistent")]
        [DefaultValue(true)]
        public bool MD004 { get; set; } = true;

        [Category("2. Lists")]
        [DisplayName("MD005 - list-indent")]
        [Description("Inconsistent indentation for list items at the same level")]
        [DefaultValue(true)]
        public bool MD005 { get; set; } = true;

        [Category("2. Lists")]
        [DisplayName("MD007 - ul-indent")]
        [Description("Unordered list indentation")]
        [DefaultValue(true)]
        public bool MD007 { get; set; } = true;

        [Category("2. Lists")]
        [DisplayName("MD029 - ol-prefix")]
        [Description("Ordered list item prefix")]
        [DefaultValue(true)]
        public bool MD029 { get; set; } = true;

        [Category("2. Lists")]
        [DisplayName("MD030 - list-marker-space")]
        [Description("Spaces after list markers")]
        [DefaultValue(true)]
        public bool MD030 { get; set; } = true;

        [Category("2. Lists")]
        [DisplayName("MD032 - blanks-around-lists")]
        [Description("Lists should be surrounded by blank lines")]
        [DefaultValue(true)]
        public bool MD032 { get; set; } = true;

        // ===== Whitespace Rules =====

        [Category("3. Whitespace")]
        [DisplayName("MD009 - no-trailing-spaces")]
        [Description("Trailing spaces")]
        [DefaultValue(true)]
        public bool MD009 { get; set; } = true;

        [Category("3. Whitespace")]
        [DisplayName("MD010 - no-hard-tabs")]
        [Description("Hard tabs")]
        [DefaultValue(true)]
        public bool MD010 { get; set; } = true;

        [Category("3. Whitespace")]
        [DisplayName("MD012 - no-multiple-blanks")]
        [Description("Multiple consecutive blank lines")]
        [DefaultValue(true)]
        public bool MD012 { get; set; } = true;

        [Category("3. Whitespace")]
        [DisplayName("MD013 - line-length")]
        [Description("Line length")]
        [DefaultValue(false)]
        public bool MD013 { get; set; } = false;

        [Category("3. Whitespace")]
        [DisplayName("MD047 - single-trailing-newline")]
        [Description("Files should end with a single newline character")]
        [DefaultValue(true)]
        public bool MD047 { get; set; } = true;

        // ===== Code Block Rules =====

        [Category("4. Code Blocks")]
        [DisplayName("MD014 - commands-show-output")]
        [Description("Dollar signs used before commands without showing output")]
        [DefaultValue(true)]
        public bool MD014 { get; set; } = true;

        [Category("4. Code Blocks")]
        [DisplayName("MD031 - blanks-around-fences")]
        [Description("Fenced code blocks should be surrounded by blank lines")]
        [DefaultValue(true)]
        public bool MD031 { get; set; } = true;

        [Category("4. Code Blocks")]
        [DisplayName("MD040 - fenced-code-language")]
        [Description("Fenced code blocks should have a language specified")]
        [DefaultValue(true)]
        public bool MD040 { get; set; } = true;

        [Category("4. Code Blocks")]
        [DisplayName("MD046 - code-block-style")]
        [Description("Code block style")]
        [DefaultValue(true)]
        public bool MD046 { get; set; } = true;

        [Category("4. Code Blocks")]
        [DisplayName("MD048 - code-fence-style")]
        [Description("Code fence style")]
        [DefaultValue(true)]
        public bool MD048 { get; set; } = true;

        // ===== Link Rules =====

        [Category("5. Links")]
        [DisplayName("MD011 - no-reversed-links")]
        [Description("Reversed link syntax")]
        [DefaultValue(true)]
        public bool MD011 { get; set; } = true;

        [Category("5. Links")]
        [DisplayName("MD034 - no-bare-urls")]
        [Description("Bare URL used")]
        [DefaultValue(true)]
        public bool MD034 { get; set; } = true;

        [Category("5. Links")]
        [DisplayName("MD039 - no-space-in-links")]
        [Description("Spaces inside link text")]
        [DefaultValue(true)]
        public bool MD039 { get; set; } = true;

        [Category("5. Links")]
        [DisplayName("MD042 - no-empty-links")]
        [Description("No empty links")]
        [DefaultValue(true)]
        public bool MD042 { get; set; } = true;

        [Category("5. Links")]
        [DisplayName("MD045 - no-alt-text")]
        [Description("Images should have alternate text (alt text)")]
        [DefaultValue(true)]
        public bool MD045 { get; set; } = true;

        [Category("5. Links")]
        [DisplayName("MD051 - link-fragments")]
        [Description("Link fragments should be valid")]
        [DefaultValue(true)]
        public bool MD051 { get; set; } = true;

        [Category("5. Links")]
        [DisplayName("MD052 - reference-links-images")]
        [Description("Reference links and images should use a label that is defined")]
        [DefaultValue(false)]
        public bool MD052 { get; set; } = false;

        [Category("5. Links")]
        [DisplayName("MD053 - link-image-reference-definitions")]
        [Description("Link and image reference definitions should be needed")]
        [DefaultValue(true)]
        public bool MD053 { get; set; } = true;

        [Category("5. Links")]
        [DisplayName("MD054 - link-image-style")]
        [Description("Link and image style")]
        [DefaultValue(true)]
        public bool MD054 { get; set; } = true;

        // ===== Inline Rules =====

        [Category("6. Inline")]
        [DisplayName("MD033 - no-inline-html")]
        [Description("Inline HTML")]
        [DefaultValue(false)]
        public bool MD033 { get; set; } = false;

        [Category("6. Inline")]
        [DisplayName("MD035 - hr-style")]
        [Description("Horizontal rule style")]
        [DefaultValue(true)]
        public bool MD035 { get; set; } = true;

        [Category("6. Inline")]
        [DisplayName("MD036 - no-emphasis-as-heading")]
        [Description("Emphasis used instead of a heading")]
        [DefaultValue(false)]
        public bool MD036 { get; set; } = false;

        [Category("6. Inline")]
        [DisplayName("MD037 - no-space-in-emphasis")]
        [Description("Spaces inside emphasis markers")]
        [DefaultValue(true)]
        public bool MD037 { get; set; } = true;

        [Category("6. Inline")]
        [DisplayName("MD038 - no-space-in-code")]
        [Description("Spaces inside code span elements")]
        [DefaultValue(true)]
        public bool MD038 { get; set; } = true;

        [Category("6. Inline")]
        [DisplayName("MD049 - emphasis-style")]
        [Description("Emphasis style should be consistent")]
        [DefaultValue(true)]
        public bool MD049 { get; set; } = true;

        [Category("6. Inline")]
        [DisplayName("MD050 - strong-style")]
        [Description("Strong style should be consistent")]
        [DefaultValue(true)]
        public bool MD050 { get; set; } = true;

        // ===== Blockquote Rules =====

        [Category("7. Blockquotes")]
        [DisplayName("MD027 - no-multiple-space-blockquote")]
        [Description("Multiple spaces after blockquote symbol")]
        [DefaultValue(true)]
        public bool MD027 { get; set; } = true;

        [Category("7. Blockquotes")]
        [DisplayName("MD028 - no-blanks-blockquote")]
        [Description("Blank line inside blockquote")]
        [DefaultValue(true)]
        public bool MD028 { get; set; } = true;

        // ===== Table Rules =====

        [Category("8. Tables")]
        [DisplayName("MD055 - table-pipe-style")]
        [Description("Table pipe style")]
        [DefaultValue(true)]
        public bool MD055 { get; set; } = true;

        [Category("8. Tables")]
        [DisplayName("MD056 - table-column-count")]
        [Description("Table column count")]
        [DefaultValue(true)]
        public bool MD056 { get; set; } = true;

        [Category("8. Tables")]
        [DisplayName("MD058 - blanks-around-tables")]
        [Description("Tables should be surrounded by blank lines")]
        [DefaultValue(true)]
        public bool MD058 { get; set; } = true;

        [Category("8. Tables")]
        [DisplayName("MD060 - table-column-style")]
        [Description("Table column style should be consistent")]
        [DefaultValue(true)]
        public bool MD060 { get; set; } = true;

        // ===== Accessibility Rules =====

        [Category("9. Accessibility")]
        [DisplayName("MD059 - descriptive-link-text")]
        [Description("Link text should be descriptive")]
        [DefaultValue(true)]
        public bool MD059 { get; set; } = true;
    }

    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class RuleOptionsPage : BaseOptionPage<RuleOptions> { }
    }
}
