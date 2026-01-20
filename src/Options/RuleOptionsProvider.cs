using System.Collections.Generic;

namespace MarkdownLintVS.Options
{
    /// <summary>
    /// Provides access to rule options for the markdown lint analyzer.
    /// Returns whether a rule is enabled based on the options page when no .editorconfig setting exists.
    /// </summary>
    public static class RuleOptionsProvider
    {
        /// <summary>
        /// Gets whether a rule is enabled from the options page.
        /// </summary>
        /// <param name="ruleId">The rule ID (e.g., "MD001").</param>
        /// <returns>True if the rule is enabled, false if disabled.</returns>
        public static bool IsRuleEnabled(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId))
                return true;

            RuleOptions options;
            try
            {
                options = RuleOptions.Instance;
            }
            catch
            {
                // Options not available (e.g., in unit tests without VS Shell)
                return true;
            }

            var id = ruleId.ToUpperInvariant();

            return id switch
            {
                // Heading rules
                "MD001" => options.MD001,
                "MD003" => options.MD003,
                "MD018" => options.MD018,
                "MD019" => options.MD019,
                "MD020" => options.MD020,
                "MD021" => options.MD021,
                "MD022" => options.MD022,
                "MD023" => options.MD023,
                "MD024" => options.MD024,
                "MD025" => options.MD025,
                "MD026" => options.MD026,
                "MD041" => options.MD041,

                // List rules
                "MD004" => options.MD004,
                "MD005" => options.MD005,
                "MD007" => options.MD007,
                "MD029" => options.MD029,
                "MD030" => options.MD030,
                "MD032" => options.MD032,

                // Whitespace rules
                "MD009" => options.MD009,
                "MD010" => options.MD010,
                "MD012" => options.MD012,
                "MD013" => options.MD013,
                "MD047" => options.MD047,

                // Code block rules
                "MD014" => options.MD014,
                "MD031" => options.MD031,
                "MD040" => options.MD040,
                "MD046" => options.MD046,
                "MD048" => options.MD048,

                // Link rules
                "MD011" => options.MD011,
                "MD034" => options.MD034,
                "MD039" => options.MD039,
                "MD042" => options.MD042,
                "MD045" => options.MD045,
                "MD051" => options.MD051,
                "MD052" => options.MD052,
                "MD053" => options.MD053,
                "MD054" => options.MD054,

                // Inline rules
                "MD033" => options.MD033,
                "MD035" => options.MD035,
                "MD036" => options.MD036,
                "MD037" => options.MD037,
                "MD038" => options.MD038,
                "MD049" => options.MD049,
                "MD050" => options.MD050,

                // Blockquote rules
                "MD027" => options.MD027,
                "MD028" => options.MD028,

                // Table rules
                "MD055" => options.MD055,
                "MD056" => options.MD056,
                "MD058" => options.MD058,

                // Default: enabled
                _ => true
            };
        }

        /// <summary>
        /// Gets all rule IDs that are enabled in the options.
        /// </summary>
        public static IEnumerable<string> GetEnabledRuleIds()
        {
            RuleOptions options = RuleOptions.Instance;

            // Heading rules
            if (options.MD001) yield return "MD001";
            if (options.MD003) yield return "MD003";
            if (options.MD018) yield return "MD018";
            if (options.MD019) yield return "MD019";
            if (options.MD020) yield return "MD020";
            if (options.MD021) yield return "MD021";
            if (options.MD022) yield return "MD022";
            if (options.MD023) yield return "MD023";
            if (options.MD024) yield return "MD024";
            if (options.MD025) yield return "MD025";
            if (options.MD026) yield return "MD026";
            if (options.MD041) yield return "MD041";

            // List rules
            if (options.MD004) yield return "MD004";
            if (options.MD005) yield return "MD005";
            if (options.MD007) yield return "MD007";
            if (options.MD029) yield return "MD029";
            if (options.MD030) yield return "MD030";
            if (options.MD032) yield return "MD032";

            // Whitespace rules
            if (options.MD009) yield return "MD009";
            if (options.MD010) yield return "MD010";
            if (options.MD012) yield return "MD012";
            if (options.MD013) yield return "MD013";
            if (options.MD047) yield return "MD047";

            // Code block rules
            if (options.MD014) yield return "MD014";
            if (options.MD031) yield return "MD031";
            if (options.MD040) yield return "MD040";
            if (options.MD046) yield return "MD046";
            if (options.MD048) yield return "MD048";

            // Link rules
            if (options.MD011) yield return "MD011";
            if (options.MD034) yield return "MD034";
            if (options.MD039) yield return "MD039";
            if (options.MD042) yield return "MD042";
            if (options.MD045) yield return "MD045";
            if (options.MD051) yield return "MD051";
            if (options.MD052) yield return "MD052";
            if (options.MD053) yield return "MD053";
            if (options.MD054) yield return "MD054";

            // Inline rules
            if (options.MD033) yield return "MD033";
            if (options.MD035) yield return "MD035";
            if (options.MD036) yield return "MD036";
            if (options.MD037) yield return "MD037";
            if (options.MD038) yield return "MD038";
            if (options.MD049) yield return "MD049";
            if (options.MD050) yield return "MD050";

            // Blockquote rules
            if (options.MD027) yield return "MD027";
            if (options.MD028) yield return "MD028";

            // Table rules
            if (options.MD055) yield return "MD055";
            if (options.MD056) yield return "MD056";
            if (options.MD058) yield return "MD058";
        }
    }
}
