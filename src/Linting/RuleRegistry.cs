using System.Collections.Generic;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Registry containing all markdown lint rule definitions.
    /// </summary>
    public static class RuleRegistry
    {
        private static readonly Dictionary<string, RuleInfo> _rulesById = [];
        private static readonly Dictionary<string, RuleInfo> _rulesByAlias = [];

        static RuleRegistry()
        {
            RegisterAllRules();
        }

        public static IEnumerable<RuleInfo> AllRules => _rulesById.Values;

        public static RuleInfo GetRule(string idOrAlias)
        {
            if (string.IsNullOrEmpty(idOrAlias))
                return null;

            var key = idOrAlias.ToUpperInvariant();
            if (_rulesById.TryGetValue(key, out RuleInfo rule))
                return rule;

            var aliasKey = idOrAlias.ToLowerInvariant().Replace("-", "_");
            if (_rulesByAlias.TryGetValue(aliasKey, out rule))
                return rule;

            return null;
        }

        private static void Register(RuleInfo rule)
        {
            _rulesById[rule.Id.ToUpperInvariant()] = rule;
            foreach (var alias in rule.Aliases)
            {
                _rulesByAlias[alias.ToLowerInvariant().Replace("-", "_")] = rule;
            }
        }

        private static void RegisterAllRules()
        {
            Register(new RuleInfo("MD001", "heading-increment", ["heading_increment"],
                "Heading levels should only increment by one level at a time"));

            Register(new RuleInfo("MD003", "heading-style", ["heading_style"],
                "Heading style should be consistent"));

            Register(new RuleInfo("MD004", "ul-style", ["ul_style"],
                "Unordered list style should be consistent"));

            Register(new RuleInfo("MD005", "list-indent", ["list_indent"],
                "Inconsistent indentation for list items at the same level"));

            Register(new RuleInfo("MD007", "ul-indent", ["ul_indent"],
                "Unordered list indentation"));

            Register(new RuleInfo("MD009", "no-trailing-spaces", ["no_trailing_spaces"],
                "Trailing spaces"));

            Register(new RuleInfo("MD010", "no-hard-tabs", ["no_hard_tabs"],
                "Hard tabs"));

            Register(new RuleInfo("MD011", "no-reversed-links", ["no_reversed_links"],
                "Reversed link syntax"));

            Register(new RuleInfo("MD012", "no-multiple-blanks", ["no_multiple_blanks"],
                "Multiple consecutive blank lines"));

            Register(new RuleInfo("MD013", "line-length", ["line_length"],
                "Line length"));

            Register(new RuleInfo("MD014", "commands-show-output", ["commands_show_output"],
                "Dollar signs used before commands without showing output"));

            Register(new RuleInfo("MD018", "no-missing-space-atx", ["no_missing_space_atx"],
                "No space after hash on atx style heading"));

            Register(new RuleInfo("MD019", "no-multiple-space-atx", ["no_multiple_space_atx"],
                "Multiple spaces after hash on atx style heading"));

            Register(new RuleInfo("MD020", "no-missing-space-closed-atx", ["no_missing_space_closed_atx"],
                "No space inside hashes on closed atx style heading"));

            Register(new RuleInfo("MD021", "no-multiple-space-closed-atx", ["no_multiple_space_closed_atx"],
                "Multiple spaces inside hashes on closed atx style heading"));

            Register(new RuleInfo("MD022", "blanks-around-headings", ["blanks_around_headings"],
                "Headings should be surrounded by blank lines"));

            Register(new RuleInfo("MD023", "heading-start-left", ["heading_start_left"],
                "Headings must start at the beginning of the line"));

            Register(new RuleInfo("MD024", "no-duplicate-heading", ["no_duplicate_heading"],
                "Multiple headings with the same content"));

            Register(new RuleInfo("MD025", "single-title", ["single_title"],
                "Multiple top-level headings in the same document"));

            Register(new RuleInfo("MD026", "no-trailing-punctuation", ["no_trailing_punctuation"],
                "Trailing punctuation in heading"));

            Register(new RuleInfo("MD027", "no-multiple-space-blockquote", ["no_multiple_space_blockquote"],
                "Multiple spaces after blockquote symbol"));

            Register(new RuleInfo("MD028", "no-blanks-blockquote", ["no_blanks_blockquote"],
                "Blank line inside blockquote"));

            Register(new RuleInfo("MD029", "ol-prefix", ["ol_prefix"],
                "Ordered list item prefix"));

            Register(new RuleInfo("MD030", "list-marker-space", ["list_marker_space"],
                "Spaces after list markers"));

            Register(new RuleInfo("MD031", "blanks-around-fences", ["blanks_around_fences"],
                "Fenced code blocks should be surrounded by blank lines"));

            Register(new RuleInfo("MD032", "blanks-around-lists", ["blanks_around_lists"],
                "Lists should be surrounded by blank lines"));

            Register(new RuleInfo("MD033", "no-inline-html", ["no_inline_html"],
                "Inline HTML"));

            Register(new RuleInfo("MD034", "no-bare-urls", ["no_bare_urls"],
                "Bare URL used"));

            Register(new RuleInfo("MD035", "hr-style", ["hr_style"],
                "Horizontal rule style"));

            Register(new RuleInfo("MD036", "no-emphasis-as-heading", ["no_emphasis_as_heading"],
                "Emphasis used instead of a heading"));

            Register(new RuleInfo("MD037", "no-space-in-emphasis", ["no_space_in_emphasis"],
                "Spaces inside emphasis markers"));

            Register(new RuleInfo("MD038", "no-space-in-code", ["no_space_in_code"],
                "Spaces inside code span elements"));

            Register(new RuleInfo("MD039", "no-space-in-links", ["no_space_in_links"],
                "Spaces inside link text"));

            Register(new RuleInfo("MD040", "fenced-code-language", ["fenced_code_language"],
                "Fenced code blocks should have a language specified"));

            Register(new RuleInfo("MD041", "first-line-heading", ["first_line_heading"],
                "First line in a file should be a top-level heading"));

            Register(new RuleInfo("MD042", "no-empty-links", ["no_empty_links"],
                "No empty links"));

            Register(new RuleInfo("MD043", "required-headings", ["required_headings"],
                "Required heading structure", DiagnosticSeverity.Warning, enabledByDefault: false));

            Register(new RuleInfo("MD044", "proper-names", ["proper_names"],
                "Proper names should have correct capitalization", DiagnosticSeverity.Warning, enabledByDefault: false));

            Register(new RuleInfo("MD045", "no-alt-text", ["no_alt_text"],
                "Images should have alternate text (alt text)"));

            Register(new RuleInfo("MD046", "code-block-style", ["code_block_style"],
                "Code block style"));

            Register(new RuleInfo("MD047", "single-trailing-newline", ["single_trailing_newline"],
                "Files should end with a single newline character"));

            Register(new RuleInfo("MD048", "code-fence-style", ["code_fence_style"],
                "Code fence style"));

            Register(new RuleInfo("MD049", "emphasis-style", ["emphasis_style"],
                "Emphasis style should be consistent"));

            Register(new RuleInfo("MD050", "strong-style", ["strong_style"],
                "Strong style should be consistent"));

            Register(new RuleInfo("MD051", "link-fragments", ["link_fragments"],
                "Link fragments should be valid"));

            Register(new RuleInfo("MD052", "reference-links-images", ["reference_links_images"],
                "Reference links and images should use a label that is defined"));

            Register(new RuleInfo("MD053", "link-image-reference-definitions", ["link_image_reference_definitions"],
                "Link and image reference definitions should be needed"));

            Register(new RuleInfo("MD054", "link-image-style", ["link_image_style"],
                "Link and image style"));

            Register(new RuleInfo("MD055", "table-pipe-style", ["table_pipe_style"],
                "Table pipe style"));

            Register(new RuleInfo("MD056", "table-column-count", ["table_column_count"],
                "Table column count"));

            Register(new RuleInfo("MD058", "blanks-around-tables", ["blanks_around_tables"],
                "Tables should be surrounded by blank lines"));
        }
    }
}
