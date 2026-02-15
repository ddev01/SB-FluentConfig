using System;
using FluentConfig.Elements;

namespace FluentConfig.Core
{
    /// <summary>
    /// Represents a visibility condition: either toggle-based (single toggle key) or predicate-based (dependency keys + predicate).
    /// </summary>
    public sealed class VisibilityCondition
    {
        public string ToggleKey { get; }
        public string[] DependencyKeys { get; }
        public Func<IRenderContext, bool> Predicate { get; }

        public bool IsToggleBased => ToggleKey != null;

        private VisibilityCondition(string toggleKey, string[] dependencyKeys, Func<IRenderContext, bool> predicate)
        {
            ToggleKey = toggleKey;
            DependencyKeys = dependencyKeys ?? Array.Empty<string>();
            Predicate = predicate;
        }

        /// <summary>
        /// Creates a toggle-based condition: visible when the toggle with the given key is checked.
        /// </summary>
        public static VisibilityCondition Toggle(string toggleKey)
        {
            if (string.IsNullOrEmpty(toggleKey))
                return null;
            return new VisibilityCondition(
                toggleKey,
                new[] { toggleKey },
                ctx => ctx.GetPendingValue<bool>(toggleKey));
        }

        /// <summary>
        /// Creates a predicate-based condition: visible when the predicate returns true.
        /// dependencyKeys are used to subscribe to control change events for re-evaluation.
        /// </summary>
        public static VisibilityCondition FromPredicate(string[] dependencyKeys, Func<IRenderContext, bool> predicate)
        {
            if (dependencyKeys == null || dependencyKeys.Length == 0 || predicate == null)
                return null;
            return new VisibilityCondition(null, dependencyKeys, predicate);
        }
    }
}
