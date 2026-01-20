namespace MarkdownLintVS.Linting
{
    /// <summary>
    /// Represents a diagnostic severity level for markdown lint rules.
    /// Maps to EditorConfig severity values and Visual Studio error types.
    /// </summary>
    public enum DiagnosticSeverity
    {
        /// <summary>Rule is disabled</summary>
        None,
        /// <summary>Silent - rule runs but doesn't report</summary>
        Silent,
        /// <summary>Suggestion - shown as message/hint</summary>
        Suggestion,
        /// <summary>Warning - shown as warning squiggle</summary>
        Warning,
        /// <summary>Error - shown as error squiggle</summary>
        Error
    }

    /// <summary>
    /// Contains information about a markdown lint rule.
    /// </summary>
    public class RuleInfo(
        string id,
        string name,
        string[] aliases,
        string description,
        DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Warning,
        bool enabledByDefault = true)
    {
        public string Id { get; } = id ?? throw new ArgumentNullException(nameof(id));
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        public string[] Aliases { get; } = aliases ?? [];
        public string Description { get; } = description ?? throw new ArgumentNullException(nameof(description));
        public DiagnosticSeverity DefaultSeverity { get; } = defaultSeverity;
        public string DocumentationUrl { get; } = $"https://github.com/DavidAnson/markdownlint/blob/main/doc/{id.ToLowerInvariant()}.md";
        public bool EnabledByDefault { get; } = enabledByDefault;
    }

    /// <summary>
    /// Represents a lint violation found in a markdown document.
    /// </summary>
    public class LintViolation(
        RuleInfo rule,
        int lineNumber,
        int columnStart,
        int columnEnd,
        string message,
        DiagnosticSeverity severity,
        string fixDescription = null)
    {
        public RuleInfo Rule { get; } = rule ?? throw new ArgumentNullException(nameof(rule));
        public int LineNumber { get; } = lineNumber;
        public int ColumnStart { get; } = columnStart;
        public int ColumnEnd { get; } = columnEnd;
        public string Message { get; } = message ?? throw new ArgumentNullException(nameof(message));
        public DiagnosticSeverity Severity { get; } = severity;
        public string FixDescription { get; } = fixDescription;

        public string GetErrorCode() => Rule.Id;
    }
}
