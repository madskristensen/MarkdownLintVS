using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using MarkdownLintVS.Options;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Registry containing all markdown lint rule definitions.
    /// Rules are automatically discovered from RuleOptions via reflection.
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
            // Discover rules from RuleOptions properties via reflection
            foreach (PropertyInfo prop in typeof(RuleOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Only process properties starting with "MD"
                if (!prop.Name.StartsWith("MD"))
                    continue;

                DisplayNameAttribute displayName = prop.GetCustomAttribute<DisplayNameAttribute>();
                DescriptionAttribute description = prop.GetCustomAttribute<DescriptionAttribute>();
                DefaultValueAttribute defaultValue = prop.GetCustomAttribute<DefaultValueAttribute>();

                if (displayName == null || description == null)
                    continue;

                // Parse "MD001 - heading-increment" into id and alias
                var parts = displayName.DisplayName.Split([" - "], System.StringSplitOptions.None);
                if (parts.Length != 2)
                    continue;

                var id = parts[0].Trim();
                var alias = parts[1].Trim();
                var enabledByDefault = defaultValue?.Value is bool b && b;

                Register(new RuleInfo(
                    id,
                    alias,
                    [alias.Replace("-", "_")],
                    description.Description,
                    enabledByDefault: enabledByDefault));
            }
        }
    }
}
