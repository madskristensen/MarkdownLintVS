using System.Collections.Generic;
using System.Reflection;
using MarkdownLintVS.Linting;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Options
{
    /// <summary>
    /// Provides access to rule options for the markdown lint analyzer.
    /// Returns whether a rule is enabled based on the options page when no .editorconfig setting exists.
    /// Uses compiled property accessors to avoid reflection overhead during analysis.
    /// </summary>
    public class RuleOptionsProvider
    {
        private static RuleOptionsProvider _instance;
        public static RuleOptionsProvider Instance => _instance ??= new RuleOptionsProvider();

        private readonly Dictionary<string, Func<RuleOptions, bool>> _ruleAccessors;
        private readonly List<string> _ruleIds;

        private RuleOptionsProvider()
        {
            (_ruleAccessors, _ruleIds) = BuildRuleAccessorCache();

            // Subscribe to options changes to be aware of when they change
            RuleOptions.Saved += OnRuleOptionsSaved;
        }

        private void OnRuleOptionsSaved(RuleOptions options)
        {
            // Options have changed - any cached configurations should be refreshed by callers
            // This is a notification hook for future caching if needed
        }

        private static (Dictionary<string, Func<RuleOptions, bool>>, List<string>) BuildRuleAccessorCache()
        {
            var accessors = new Dictionary<string, Func<RuleOptions, bool>>(StringComparer.OrdinalIgnoreCase);
            var ruleIds = new List<string>();

            foreach (PropertyInfo prop in typeof(RuleOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Rule properties are named like "MD001", "MD003", etc.
                if (prop.PropertyType == typeof(bool) && prop.Name.StartsWith("MD", StringComparison.OrdinalIgnoreCase))
                {
                    // Create a compiled delegate for fast property access (avoids reflection on each call)
                    var getter = (Func<RuleOptions, bool>)Delegate.CreateDelegate(
                        typeof(Func<RuleOptions, bool>),
                        prop.GetGetMethod());

                    accessors[prop.Name] = getter;
                    ruleIds.Add(prop.Name);
                }
            }

            return (accessors, ruleIds);
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

            if (Instance._ruleAccessors.TryGetValue(ruleId, out Func<RuleOptions, bool> accessor))
            {
                return accessor(options);
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

            foreach (var ruleId in Instance._ruleIds)
            {
                if (Instance._ruleAccessors[ruleId](options))
                {
                    yield return ruleId;
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

            foreach (var ruleId in _ruleIds)
            {
                var enabled = _ruleAccessors[ruleId](options);
                RuleInfo ruleInfo = RuleRegistry.GetRule(ruleId);

                configs[ruleId] = new RuleConfiguration
                {
                    Enabled = enabled,
                    Severity = ruleInfo?.DefaultSeverity ?? DiagnosticSeverity.Warning
                };
            }

            return configs;
        }
    }
}
