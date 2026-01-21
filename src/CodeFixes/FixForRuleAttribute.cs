using System;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Specifies which span to use when creating the fix action.
    /// </summary>
    public enum FixSpanType
    {
        /// <summary>Use the entire line span.</summary>
        Line,
        /// <summary>Use the violation's column span.</summary>
        Violation
    }

    /// <summary>
    /// Marks a fix action class as the handler for a specific markdown lint rule.
    /// The fix action will be automatically discovered and registered.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class FixForRuleAttribute : Attribute
    {
        /// <summary>
        /// Gets the rule ID this fix action handles (e.g., "MD009").
        /// </summary>
        public string RuleId { get; }

        /// <summary>
        /// Gets or sets which span type to use when creating the fix action.
        /// Default is <see cref="FixSpanType.Line"/>.
        /// </summary>
        public FixSpanType SpanType { get; set; } = FixSpanType.Line;

        /// <summary>
        /// Gets or sets whether this fix requires custom factory logic.
        /// If true, the fix action class must implement a static Create method.
        /// </summary>
        public bool RequiresFactory { get; set; } = false;

        /// <summary>
        /// Creates a new instance of the attribute for the specified rule.
        /// </summary>
        /// <param name="ruleId">The rule ID this fix action handles (e.g., "MD009").</param>
        public FixForRuleAttribute(string ruleId)
        {
            RuleId = ruleId ?? throw new ArgumentNullException(nameof(ruleId));
        }
    }
}
