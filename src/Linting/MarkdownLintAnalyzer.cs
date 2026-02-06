using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EditorConfig.Core;
using MarkdownLintVS.Linting.Rules;
using MarkdownLintVS.Options;

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

        /// <summary>
        /// TTL cache for EditorConfig configurations. Avoids re-parsing .editorconfig
        /// files from disk on every debounced keystroke. Entries expire after <see cref="_cacheTtl"/>.
        /// </summary>
        private readonly ConcurrentDictionary<string, CachedEditorConfig> _editorConfigCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);

        public MarkdownLintAnalyzer()
        {
            _rules = CreateRules();
            _editorConfigParser = new EditorConfigParser();
        }

        /// <summary>
        /// Gets the list of all registered rules.
        /// </summary>
        public IReadOnlyList<IMarkdownRule> Rules => _rules;

        /// <summary>
        /// Clears the EditorConfig configuration cache, forcing a fresh parse on the next analysis.
        /// Call this when a .editorconfig file is saved.
        /// </summary>
        public void ClearEditorConfigCache() => _editorConfigCache.Clear();

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

                // File link rules
                new MD061_FileLinkExists(),
                new MD062_ImageLinkExists(),

                // Table rules
                new MD055_TablePipeStyle(),
                new MD056_TableColumnCount(),
                new MD058_BlanksAroundTables(),
            ];
        }

        /// <summary>
        /// Analyzes a markdown document and returns all violations.
        /// Rules are executed in parallel for improved throughput on large documents.
        /// </summary>
        public IEnumerable<LintViolation> Analyze(string text, string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            var analysis = new MarkdownDocumentAnalysis(text, filePath);
            Dictionary<string, RuleConfiguration> configurations = GetRuleConfigurations(filePath);

            // Set root path from editorconfig and options
            SetRootPathOnAnalysis(analysis, configurations);

            return RunRulesInParallel(_rules, analysis, configurations, cancellationToken, GetConfigurationForRule);
        }

        /// <summary>
        /// Static method to analyze a markdown document using provided configurations.
        /// Used by LintFolderCommand for parallel processing.
        /// </summary>
        public static IEnumerable<LintViolation> Analyze(
            MarkdownDocumentAnalysis analysis,
            Dictionary<string, RuleConfiguration> ruleConfigs,
            Dictionary<string, RuleConfiguration> editorConfigSettings,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<IMarkdownRule> rules = Instance.Rules;

            // Set root path from editorconfig and options
            SetRootPathOnAnalysisStatic(analysis, editorConfigSettings);

            return RunRulesInParallel(
                rules,
                analysis,
                editorConfigSettings,
                cancellationToken,
                (rule, configs) => GetConfigurationForRuleStatic(rule, ruleConfigs, configs));
        }

        /// <summary>
        /// Executes enabled rules in parallel and returns unsuppressed violations.
        /// The MarkdownDocumentAnalysis is read-only after construction, so rules can safely
        /// share it across threads. Results are collected into a ConcurrentBag and returned
        /// sorted by line number for deterministic output.
        /// </summary>
        private static List<LintViolation> RunRulesInParallel(
            IReadOnlyList<IMarkdownRule> rules,
            MarkdownDocumentAnalysis analysis,
            Dictionary<string, RuleConfiguration> configurations,
            CancellationToken cancellationToken,
            Func<RuleInfo, Dictionary<string, RuleConfiguration>, RuleConfiguration> getConfig)
        {
            // Pre-filter to only enabled rules
            var enabledRules = new List<(IMarkdownRule Rule, RuleConfiguration Config)>();
            foreach (IMarkdownRule rule in rules)
            {
                RuleConfiguration config = getConfig(rule.Info, configurations);
                if (config.Enabled && config.Severity != DiagnosticSeverity.None)
                {
                    enabledRules.Add((rule, config));
                }
            }

            if (enabledRules.Count == 0)
                return [];

            var results = new ConcurrentBag<LintViolation>();

            Parallel.ForEach(enabledRules,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    CancellationToken = cancellationToken
                },
                entry =>
                {
                    try
                    {
                        foreach (LintViolation violation in entry.Rule.Analyze(
                            analysis, entry.Config, entry.Config.Severity, cancellationToken))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (!analysis.Suppressions.IsRuleSuppressed(violation.LineNumber, violation.Rule))
                            {
                                results.Add(violation);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Propagate cancellation
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Rule {entry.Rule.Info.Id} threw an exception: {ex.Message}");
                    }
                });

            cancellationToken.ThrowIfCancellationRequested();

            // Sort by line number then column for deterministic output
            return [.. results.OrderBy(v => v.LineNumber).ThenBy(v => v.ColumnStart)];
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
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return new(StringComparer.OrdinalIgnoreCase);

            // Check TTL cache first
            if (_editorConfigCache.TryGetValue(filePath, out CachedEditorConfig cached) &&
                cached.IsValid(_cacheTtl))
            {
                return cached.Configurations;
            }

            Dictionary<string, RuleConfiguration> configurations = ParseEditorConfig(filePath);

            _editorConfigCache[filePath] = new CachedEditorConfig(configurations);

            return configurations;
        }

        private Dictionary<string, RuleConfiguration> ParseEditorConfig(string filePath)
        {
            var configurations = new Dictionary<string, RuleConfiguration>(StringComparer.OrdinalIgnoreCase);
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

        internal RuleConfiguration ParseRuleConfiguration(string value)
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

            // Check if the entire value is a severity
            if (TryParseSeverity(trimmed, out DiagnosticSeverity severity))
            {
                config.Severity = severity;
                return config;
            }

            // Check for severity suffix (value:severity)
            // Only split on the last colon if the suffix is a valid severity
            var lastColonIndex = value.LastIndexOf(':');
            if (lastColonIndex > 0 && lastColonIndex < value.Length - 1)
            {
                var potentialSeverity = value.Substring(lastColonIndex + 1).Trim();
                if (TryParseSeverity(potentialSeverity.ToLowerInvariant(), out severity))
                {
                    config.Severity = severity;
                    config.Value = value.Substring(0, lastColonIndex);
                    return config;
                }
            }

            // No severity suffix found - treat the entire value as the configuration value
            config.Value = value;
            return config;
        }

        private static bool TryParseSeverity(string value, out DiagnosticSeverity severity)
        {
            switch (value)
            {
                case "error":
                    severity = DiagnosticSeverity.Error;
                    return true;
                case "warning":
                    severity = DiagnosticSeverity.Warning;
                    return true;
                case "suggestion":
                case "info":
                case "information":
                case "hint":
                    severity = DiagnosticSeverity.Suggestion;
                    return true;
                case "silent":
                case "refactoring":
                    severity = DiagnosticSeverity.Silent;
                    return true;
                case "none":
                    severity = DiagnosticSeverity.None;
                    return true;
                default:
                    severity = DiagnosticSeverity.Warning;
                    return false;
            }
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

        /// <summary>
        /// Sets the root path on the analysis from editorconfig and options.
        /// </summary>
        private void SetRootPathOnAnalysis(MarkdownDocumentAnalysis analysis, Dictionary<string, RuleConfiguration> configurations)
        {
            // Get root path from editorconfig
            if (configurations != null && configurations.TryGetValue("root_path", out RuleConfiguration rootPathConfig))
            {
                analysis.EditorConfigRootPath = rootPathConfig.Value;
            }

            // Get root path from options page
            try
            {
                analysis.OptionsRootPath = RuleOptions.Instance?.RootPath;
            }
            catch
            {
                // Options not available (e.g., in unit tests without VS Shell)
            }
        }

        /// <summary>
        /// Static version of SetRootPathOnAnalysis for parallel processing.
        /// </summary>
        private static void SetRootPathOnAnalysisStatic(
            MarkdownDocumentAnalysis analysis,
            Dictionary<string, RuleConfiguration> editorConfigSettings)
        {
            // Get root path from editorconfig
            if (editorConfigSettings != null && editorConfigSettings.TryGetValue("root_path", out RuleConfiguration rootPathConfig))
            {
                analysis.EditorConfigRootPath = rootPathConfig.Value;
            }

            // Get root path from options page
            try
            {
                analysis.OptionsRootPath = RuleOptions.Instance?.RootPath;
            }
            catch
            {
                // Options not available (e.g., in unit tests without VS Shell)
            }
        }
    }

    /// <summary>
    /// Cached EditorConfig result with a timestamp for TTL-based expiration.
    /// </summary>
    internal sealed class CachedEditorConfig(Dictionary<string, RuleConfiguration> configurations)
    {
        private readonly DateTime _createdUtc = DateTime.UtcNow;

        public Dictionary<string, RuleConfiguration> Configurations { get; } = configurations;

        public bool IsValid(TimeSpan ttl) => DateTime.UtcNow - _createdUtc < ttl;
    }
}
