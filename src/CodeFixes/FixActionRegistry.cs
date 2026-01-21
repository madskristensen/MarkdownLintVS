using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MarkdownLintVS.CodeFixes.Actions;
using MarkdownLintVS.Linting;
using Microsoft.VisualStudio.Text;

namespace MarkdownLintVS.CodeFixes
{
    /// <summary>
    /// Registry that auto-discovers and manages fix actions for markdown lint rules.
    /// Fix actions are discovered via the <see cref="FixForRuleAttribute"/> attribute.
    /// </summary>
    public static class FixActionRegistry
    {
        private static readonly Dictionary<string, FixActionInfo> _registry;

        static FixActionRegistry()
        {
            _registry = DiscoverFixActions();
        }

        /// <summary>
        /// Returns whether a rule has an auto-fix available.
        /// </summary>
        public static bool HasFix(string ruleId)
        {
            return _registry.ContainsKey(ruleId);
        }

        /// <summary>
        /// Gets all registered rule IDs that have fixes.
        /// </summary>
        public static IEnumerable<string> RegisteredRuleIds => _registry.Keys;

        /// <summary>
        /// Creates a fix action for a violation, or null if no fix is available or applicable.
        /// </summary>
        public static MarkdownFixAction CreateFix(
            LintViolation violation,
            ITextSnapshot snapshot,
            ITextSnapshotLine line)
        {
            if (!_registry.TryGetValue(violation.Rule.Id, out var info))
                return null;

            var span = info.SpanType == FixSpanType.Line
                ? new Span(line.Start, line.Length)
                : new Span(line.Start + violation.ColumnStart, violation.ColumnEnd - violation.ColumnStart);

            return info.CreateAction(snapshot, span, violation);
        }

        private static Dictionary<string, FixActionInfo> DiscoverFixActions()
        {
            var result = new Dictionary<string, FixActionInfo>(StringComparer.OrdinalIgnoreCase);

            var fixActionTypes = typeof(MarkdownFixAction).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(MarkdownFixAction).IsAssignableFrom(t));

            foreach (var type in fixActionTypes)
            {
                var attributes = type.GetCustomAttributes<FixForRuleAttribute>();
                foreach (var attr in attributes)
                {
                    try
                    {
                        var info = new FixActionInfo(type, attr);
                        result[attr.RuleId] = info;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to register fix for {attr.RuleId}: {ex.Message}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Contains information about a registered fix action.
        /// </summary>
        private class FixActionInfo
        {
            private readonly Type _type;
            private readonly Func<ITextSnapshot, Span, LintViolation, MarkdownFixAction> _factory;

            public FixSpanType SpanType { get; }

            public FixActionInfo(Type type, FixForRuleAttribute attr)
            {
                _type = type;
                SpanType = attr.SpanType;
                _factory = BuildFactory(type, attr.RequiresFactory);
            }

            public MarkdownFixAction CreateAction(ITextSnapshot snapshot, Span span, LintViolation violation)
            {
                try
                {
                    return _factory(snapshot, span, violation);
                }
                catch
                {
                    return null;
                }
            }

            private static Func<ITextSnapshot, Span, LintViolation, MarkdownFixAction> BuildFactory(
                Type type, bool requiresFactory)
            {
                if (requiresFactory)
                {
                    // Look for static Create method: MarkdownFixAction Create(ITextSnapshot, Span, LintViolation)
                    var createMethod = type.GetMethod("Create",
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        [typeof(ITextSnapshot), typeof(Span), typeof(LintViolation)],
                        null);

                    if (createMethod != null && typeof(MarkdownFixAction).IsAssignableFrom(createMethod.ReturnType))
                    {
                        return (snapshot, span, violation) =>
                            (MarkdownFixAction)createMethod.Invoke(null, [snapshot, span, violation]);
                    }

                    throw new InvalidOperationException(
                        $"Fix action {type.Name} has RequiresFactory=true but no valid static Create method.");
                }

                // Simple constructor: (ITextSnapshot, Span)
                var ctor = type.GetConstructor([typeof(ITextSnapshot), typeof(Span)]);
                if (ctor != null)
                {
                    return (snapshot, span, _) =>
                        (MarkdownFixAction)ctor.Invoke([snapshot, span]);
                }

                throw new InvalidOperationException(
                    $"Fix action {type.Name} must have a (ITextSnapshot, Span) constructor or use RequiresFactory=true with a static Create method.");
            }
        }
    }
}
