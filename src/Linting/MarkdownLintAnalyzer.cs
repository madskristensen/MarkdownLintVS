using System.Collections.Generic;
using System.IO;
using System.Linq;
using EditorConfig.Core;
using MarkdownLintVS.Linting.Rules;

namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Main analyzer service that coordinates rule execution and EditorConfig integration.
    /// </summary>
    public class MarkdownLintAnalyzer : IMarkdownLintAnalyzer
    {
        private static readonly Lazy<MarkdownLintAnalyzer> _instance =
            new(() => new MarkdownLintAnalyzer(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        public static MarkdownLintAnalyzer Instance => _instance.Value;

        private readonly List<IMarkdownRule> _rules;
        private readonly EditorConfigParser _editorConfigParser;

        public MarkdownLintAnalyzer()
        {
            _rules = CreateRules();
            _editorConfigParser = new EditorConfigParser();
        }

        /// <summary>
        /// Gets the list of all registered rules.
        /// </summary>
        public IReadOnlyList<IMarkdownRule> Rules => _rules;

        private List<IMarkdownRule> CreateRules()
        {
            return
            [
                // Heading and list rules
                new MD001_HeadingIncrement(),
                new MD003_HeadingStyle(),
                new MD004_UlStyle(),
                new MD005_ListIndent(),
                new MD007_UlIndent(),

                // Whitespace rules
                new MD009_NoTrailingSpaces(),
                new MD010_NoHardTabs(),
                new MD011_NoReversedLinks(),
                new MD012_NoMultipleBlanks(),
                new MD013_LineLength(),
                new MD014_CommandsShowOutput(),

                // Heading style rules
                new MD018_NoMissingSpaceAtx(),
                new MD019_NoMultipleSpaceAtx(),
                new MD020_NoMissingSpaceClosedAtx(),
                new MD021_NoMultipleSpaceClosedAtx(),
                new MD022_BlanksAroundHeadings(),
                new MD023_HeadingStartLeft(),
                new MD024_NoDuplicateHeading(),
                new MD025_SingleTitle(),
                new MD026_NoTrailingPunctuation(),

                // Blockquote and list rules
                new MD027_NoMultipleSpaceBlockquote(),
                new MD028_NoBlanksBlockquote(),
                new MD029_OlPrefix(),
                new MD030_ListMarkerSpace(),
                new MD031_BlanksAroundFences(),
                new MD032_BlanksAroundLists(),

                // Inline rules
                new MD033_NoInlineHtml(),
                new MD034_NoBareUrls(),
                new MD035_HrStyle(),
                new MD036_NoEmphasisAsHeading(),
                new MD037_NoSpaceInEmphasis(),
                new MD038_NoSpaceInCode(),
                new MD039_NoSpaceInLinks(),

                // Code block rules
                new MD040_FencedCodeLanguage(),
                new MD041_FirstLineHeading(),
                new MD042_NoEmptyLinks(),
                new MD045_NoAltText(),
                new MD046_CodeBlockStyle(),
                new MD047_SingleTrailingNewline(),
                new MD048_CodeFenceStyle(),

                // Link and style rules
                new MD049_EmphasisStyle(),
                new MD050_StrongStyle(),
                new MD051_LinkFragments(),
                new MD052_ReferenceLinksImages(),
                new MD053_LinkImageReferenceDefinitions(),

                // Table rules
                new MD055_TablePipeStyle(),
                new MD056_TableColumnCount(),
                new MD058_BlanksAroundTables(),
            ];
        }

        /// <summary>
        /// Analyzes a markdown document and returns all violations.
        /// </summary>
        public IEnumerable<LintViolation> Analyze(string text, string filePath)
        {
            if (string.IsNullOrEmpty(text))
                yield break;

            var analysis = new MarkdownDocumentAnalysis(text);
            Dictionary<string, RuleConfiguration> configurations = GetRuleConfigurations(filePath);

            foreach (IMarkdownRule rule in _rules)
            {
                RuleConfiguration config = GetConfigurationForRule(rule.Info, configurations);

                if (!config.Enabled || config.Severity == DiagnosticSeverity.None)
                    continue;

                IEnumerable<LintViolation> violations;
                try
                {
                    violations = rule.Analyze(analysis, config, config.Severity);
                }
                catch (Exception ex)
                {
                    ex.Log($"Rule {rule.Info.Id} threw an exception");
                    continue;
                }

                foreach (LintViolation violation in violations)
                {
                    yield return violation;
                }
            }
        }

        /// <summary>
        /// Static method to analyze a markdown document using provided configurations.
        /// Used by LintFolderCommand for parallel processing.
        /// </summary>
        public static IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            Dictionary<string, RuleConfiguration> ruleConfigs,
            Dictionary<string, RuleConfiguration> editorConfigSettings)
        {
            IReadOnlyList<IMarkdownRule> rules = Instance.Rules;

            foreach (IMarkdownRule rule in rules)
            {
                RuleConfiguration config = GetConfigurationForRuleStatic(rule.Info, ruleConfigs, editorConfigSettings);

                if (!config.Enabled || config.Severity == DiagnosticSeverity.None)
                    continue;

                IEnumerable<LintViolation> violations;
                try
                {
                    violations = rule.Analyze(analysis, config, config.Severity);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Rule {rule.Info.Id} threw an exception: {ex.Message}");
                    continue;
                }

                foreach (LintViolation violation in violations)
                {
                    yield return violation;
                }
            }
        }

        /// <summary>
        /// Gets EditorConfig settings for a directory.
        /// </summary>
        public static Dictionary<string, RuleConfiguration> GetEditorConfigSettings(string directoryPath)
        {
            var configurations = new Dictionary<string, RuleConfiguration>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(directoryPath))
                return configurations;

            int? editorConfigIndentSize = null;

            try
            {
                // Create a dummy file path in the directory to parse EditorConfig
                var dummyFilePath = Path.Combine(directoryPath, "dummy.md");
                FileConfiguration fileConfig = Instance._editorConfigParser.Parse(dummyFilePath);

                // Extract the standard EditorConfig indent_size property for use as a fallback
                if (fileConfig.IndentSize?.NumberOfColumns != null)
                {
                    editorConfigIndentSize = fileConfig.IndentSize.NumberOfColumns;
                }

                foreach (KeyValuePair<string, string> property in fileConfig.Properties)
                {
                    if (property.Key.StartsWith("md_", StringComparison.OrdinalIgnoreCase))
                    {
                        var ruleName = property.Key.Substring("md_".Length);
                        RuleConfiguration config = Instance.ParseRuleConfiguration(property.Value);
                        config.EditorConfigIndentSize = editorConfigIndentSize;
                        configurations[ruleName] = config;
                    }
                }

                // Store the indent_size for rules that may need it even without explicit md_* settings
                configurations["__editorconfig_indent_size__"] = new RuleConfiguration
                {
                    EditorConfigIndentSize = editorConfigIndentSize
                };
            }
            catch (Exception ex)
            {
                // Log EditorConfig parsing errors for debugging
                System.Diagnostics.Debug.WriteLine($"EditorConfig parsing error for {directoryPath}: {ex.Message}");
            }

            return configurations;
        }

        private static RuleConfiguration GetConfigurationForRuleStatic(
            RuleInfo rule,
            Dictionary<string, RuleConfiguration> ruleConfigs,
            Dictionary<string, RuleConfiguration> editorConfigSettings)
        {
            // Get the EditorConfig indent_size if it was stored
            int? editorConfigIndentSize = null;
            if (editorConfigSettings != null &&
                editorConfigSettings.TryGetValue("__editorconfig_indent_size__", out RuleConfiguration indentConfig))
            {
                editorConfigIndentSize = indentConfig.EditorConfigIndentSize;
            }

            // EditorConfig takes precedence
            if (editorConfigSettings != null)
            {
                if (editorConfigSettings.TryGetValue(rule.Id, out RuleConfiguration config))
                {
                    config.EditorConfigIndentSize = editorConfigIndentSize;
                    return config;
                }
                if (editorConfigSettings.TryGetValue(rule.Name, out config))
                {
                    config.EditorConfigIndentSize = editorConfigIndentSize;
                    return config;
                }
                foreach (var alias in rule.Aliases)
                {
                    if (editorConfigSettings.TryGetValue(alias, out config))
                    {
                        config.EditorConfigIndentSize = editorConfigIndentSize;
                        return config;
                    }
                }
            }

            // Fall back to options page settings
            if (ruleConfigs != null)
            {
                if (ruleConfigs.TryGetValue(rule.Id, out RuleConfiguration config))
                {
                    config.EditorConfigIndentSize = editorConfigIndentSize;
                    return config;
                }
                if (ruleConfigs.TryGetValue(rule.Name, out config))
                {
                    config.EditorConfigIndentSize = editorConfigIndentSize;
                    return config;
                }
            }

            // Default: use rule defaults
            return new RuleConfiguration
            {
                Enabled = rule.EnabledByDefault,
                Severity = rule.DefaultSeverity,
                EditorConfigIndentSize = editorConfigIndentSize
            };
        }

        private Dictionary<string, RuleConfiguration> GetRuleConfigurations(string filePath)
        {
            var configurations = new Dictionary<string, RuleConfiguration>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return configurations;

            int? editorConfigIndentSize = null;

            try
            {
                FileConfiguration fileConfig = _editorConfigParser.Parse(filePath);

                // Extract the standard EditorConfig indent_size property for use as a fallback
                if (fileConfig.IndentSize?.NumberOfColumns != null)
                {
                    editorConfigIndentSize = fileConfig.IndentSize.NumberOfColumns;
                }

                foreach (KeyValuePair<string, string> property in fileConfig.Properties)
                {
                    // Look for md_* properties (new format)
                    if (property.Key.StartsWith("md_", StringComparison.OrdinalIgnoreCase))
                    {
                        var ruleName = property.Key.Substring("md_".Length);
                        RuleConfiguration config = ParseRuleConfiguration(property.Value);
                        config.EditorConfigIndentSize = editorConfigIndentSize;
                        configurations[ruleName] = config;
                    }
                }

                // Store the indent_size for rules that may need it even without explicit md_* settings
                // Use a special key to pass this to GetConfigurationForRule
                configurations["__editorconfig_indent_size__"] = new RuleConfiguration
                {
                    EditorConfigIndentSize = editorConfigIndentSize
                };
            }
            catch (Exception ex)
            {
                ex.Log("EditorConfig parsing failed, using defaults");
            }

            return configurations;
        }

        private RuleConfiguration ParseRuleConfiguration(string value)
        {
            var config = new RuleConfiguration();

            if (string.IsNullOrEmpty(value))
                return config;

            var trimmed = value.Trim().ToLowerInvariant();

            // Handle boolean values
            if (trimmed == "false" || trimmed == "off" || trimmed == "none")
            {
                config.Enabled = false;
                return config;
            }

            if (trimmed == "true" || trimmed == "on")
            {
                config.Enabled = true;
                return config;
            }

            // Check for severity suffix (value:severity)
            var parts = value.Split(':');
            if (parts.Length >= 2)
            {
                config.Severity = ParseSeverity(parts[parts.Length - 1]);
                config.Value = string.Join(":", parts.Take(parts.Length - 1));
            }
            else
            {
                // Try to parse as severity first
                DiagnosticSeverity severity = ParseSeverity(trimmed);
                if (severity != DiagnosticSeverity.Warning)
                {
                    config.Severity = severity;
                }
                else
                {
                    config.Value = value;
                }
            }

            return config;
        }

        private DiagnosticSeverity ParseSeverity(string value)
        {
            if (string.IsNullOrEmpty(value))
                return DiagnosticSeverity.Warning;

            return value.Trim().ToLowerInvariant() switch
            {
                "error" => DiagnosticSeverity.Error,
                "warning" => DiagnosticSeverity.Warning,
                "suggestion" or "info" or "information" or "hint" => DiagnosticSeverity.Suggestion,
                "silent" or "refactoring" => DiagnosticSeverity.Silent,
                "none" or "false" or "off" => DiagnosticSeverity.None,
                _ => DiagnosticSeverity.Warning,
            };
        }

        private RuleConfiguration GetConfigurationForRule(
            RuleInfo rule,
            Dictionary<string, RuleConfiguration> configurations)
        {
            // Get the EditorConfig indent_size if it was stored
            int? editorConfigIndentSize = null;
            if (configurations.TryGetValue("__editorconfig_indent_size__", out RuleConfiguration indentConfig))
            {
                editorConfigIndentSize = indentConfig.EditorConfigIndentSize;
            }

            // Check by rule ID (MD001, MD002, etc.)
            if (configurations.TryGetValue(rule.Id, out RuleConfiguration config))
            {
                config.EditorConfigIndentSize = editorConfigIndentSize;
                return config;
            }

            // Check by rule name
            if (configurations.TryGetValue(rule.Name, out config))
            {
                config.EditorConfigIndentSize = editorConfigIndentSize;
                return config;
            }

            // Check by aliases
            foreach (var alias in rule.Aliases)
            {
                if (configurations.TryGetValue(alias, out config))
                {
                    config.EditorConfigIndentSize = editorConfigIndentSize;
                    return config;
                }
            }

            // No .editorconfig setting found - use options page as fallback
            var enabledFromOptions = Options.RuleOptionsProvider.IsRuleEnabled(rule.Id);

            return new RuleConfiguration
            {
                Enabled = enabledFromOptions && rule.EnabledByDefault,
                Severity = rule.DefaultSeverity,
                EditorConfigIndentSize = editorConfigIndentSize
            };
        }
    }
}
