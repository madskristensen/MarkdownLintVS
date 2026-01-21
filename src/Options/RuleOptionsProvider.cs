using System.Collections.Generic;
using System.Reflection;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Options
{
    /// <summary>
    /// Provides access to rule options for the markdown lint analyzer.
    /// Returns whether a rule is enabled based on the options page when no .editorconfig setting exists.
    /// Uses reflection to automatically discover rule properties from RuleOptions.
    /// </summary>
    public class RuleOptionsProvider
    {
        private static RuleOptionsProvider _instance;
        public static RuleOptionsProvider Instance => _instance ??= new RuleOptionsProvider();

        private readonly Dictionary<string, PropertyInfo> _ruleProperties;

        private RuleOptionsProvider()
        {
            _ruleProperties = BuildRulePropertyCache();
        }

        private static Dictionary<string, PropertyInfo> BuildRulePropertyCache()
        {
            var cache = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo prop in typeof(RuleOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Rule properties are named like "MD001", "MD003", etc.
                if (prop.PropertyType == typeof(bool) && prop.Name.StartsWith("MD", StringComparison.OrdinalIgnoreCase))
                {
                    cache[prop.Name] = prop;
                }
            }

            return cache;
        }

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

            if (Instance._ruleProperties.TryGetValue(ruleId, out PropertyInfo prop))
            {
                return (bool)prop.GetValue(options);
            }

            // Unknown rule ID - default to enabled
            return true;
        }

        /// <summary>
        /// Gets all rule IDs that are enabled in the options.
        /// </summary>
        public static IEnumerable<string> GetEnabledRuleIds()
        {
            RuleOptions options;
            try
            {
                options = RuleOptions.Instance;
            }
            catch
            {
                yield break;
            }

            foreach (KeyValuePair<string, PropertyInfo> kvp in Instance._ruleProperties)
            {
                if ((bool)kvp.Value.GetValue(options))
                {
                    yield return kvp.Key;
                }
            }
        }

        /// <summary>
        /// Gets rule configurations from the options page.
        /// Used by LintFolderCommand for batch processing.
        /// </summary>
        public Dictionary<string, RuleConfiguration> GetRuleConfigurations()
        {
            var configs = new Dictionary<string, RuleConfiguration>(StringComparer.OrdinalIgnoreCase);

            RuleOptions options;
            try
            {
                options = RuleOptions.Instance;
            }
            catch
            {
                return configs;
            }

            foreach (KeyValuePair<string, PropertyInfo> kvp in _ruleProperties)
            {
                var enabled = (bool)kvp.Value.GetValue(options);
                RuleInfo ruleInfo = RuleRegistry.GetRule(kvp.Key);

                configs[kvp.Key] = new RuleConfiguration
                {
                    Enabled = enabled,
                    Severity = ruleInfo?.DefaultSeverity ?? DiagnosticSeverity.Warning
                };
            }

            return configs;
        }
    }
}
