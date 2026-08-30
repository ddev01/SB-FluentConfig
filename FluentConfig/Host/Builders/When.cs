using System;
using FluentConfig.Protocol;

namespace FluentConfig
{
    /// <summary>
    /// Immutable visibility condition for <c>ShowWhen(When)</c> and <c>WithVisibility(When, ...)</c>.
    /// AND is <see cref="All"/> (nested groups on the wire). There is no OR.
    /// </summary>
    public sealed class When
    {
        internal enum Mode
        {
            On,
            Off,
            Eq,
            Neq,
            CmpLiteral,
            CmpKey,
            All,
        }

        private When(Mode kind, string key)
        {
            Kind = kind;
            Key = key;
        }

        private When(Mode kind, string key, object equalsValue)
        {
            Kind = kind;
            Key = key;
            EqualsValue = equalsValue;
        }

        private When(Mode kind, string key, Comparator op, int literal)
        {
            Kind = kind;
            Key = key;
            Op = op;
            Literal = literal;
        }

        private When(Mode kind, string key, Comparator op, string compareKey)
        {
            Kind = kind;
            Key = key;
            Op = op;
            CompareKey = compareKey;
        }

        private When(When[] children)
        {
            Kind = Mode.All;
            Children = children;
        }

        internal Mode Kind { get; }
        internal string Key { get; }
        internal object EqualsValue { get; }
        internal Comparator Op { get; }
        internal int Literal { get; }
        internal string CompareKey { get; }
        internal When[] Children { get; }
        internal bool IsAll => Kind == Mode.All;

        /// <summary>Visible when the toggle at <paramref name="key"/> is on (boolean equals true).</summary>
        public static When On(string key) => new When(Mode.On, RequireKey(key));

        /// <summary>
        /// Visible when the toggle at <paramref name="key"/> is off.
        /// Emits boolean <c>equals: true</c> with <c>inverted: true</c> (not string <c>"true"</c> —
        /// the protocol compares booleans strictly, so <c>ShowWhenNot(key, "true")</c> would not match).
        /// </summary>
        public static When Off(string key) => new When(Mode.Off, RequireKey(key));

        public static When Eq(string key, string equalsValue) =>
            new When(Mode.Eq, RequireKey(key), (object)equalsValue);

        public static When Eq(string key, int equalsValue) =>
            new When(Mode.Eq, RequireKey(key), (object)equalsValue);

        public static When Neq(string key, string equalsValue) =>
            new When(Mode.Neq, RequireKey(key), (object)equalsValue);

        public static When Neq(string key, int equalsValue) =>
            new When(Mode.Neq, RequireKey(key), (object)equalsValue);

        public static When Cmp(string key, Comparator op, int value) =>
            new When(Mode.CmpLiteral, RequireKey(key), op, value);

        public static When Cmp(string key, Comparator op, string compareKey)
        {
            if (string.IsNullOrWhiteSpace(compareKey))
                throw new ArgumentException("compareKey is required (non-null, non-empty).", nameof(compareKey));
            return new When(Mode.CmpKey, RequireKey(key), op, compareKey.Trim());
        }

        /// <summary>
        /// AND of conditions. Emits nested <c>group</c> nodes (one <c>VisibilityCondition</c> per node).
        /// There is no OR on the wire.
        /// </summary>
        public static When All(params When[] conditions)
        {
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));
            if (conditions.Length == 0)
                throw new ArgumentException("When.All requires at least one condition.", nameof(conditions));
            for (int i = 0; i < conditions.Length; i++)
            {
                if (conditions[i] == null)
                    throw new ArgumentException("When.All conditions cannot contain null.", nameof(conditions));
            }

            var copy = new When[conditions.Length];
            Array.Copy(conditions, copy, conditions.Length);
            return new When(copy);
        }

        internal VisibilityCondition ToCondition()
        {
            switch (Kind)
            {
                case Mode.On:
                    return new VisibilityCondition { SaveKey = Key, EqualsValue = true, Inverted = false };
                case Mode.Off:
                    return new VisibilityCondition { SaveKey = Key, EqualsValue = true, Inverted = true };
                case Mode.Eq:
                    return new VisibilityCondition { SaveKey = Key, EqualsValue = EqualsValue, Inverted = false };
                case Mode.Neq:
                    return new VisibilityCondition { SaveKey = Key, EqualsValue = EqualsValue, Inverted = true };
                case Mode.CmpLiteral:
                    return new VisibilityCondition
                    {
                        SaveKey = Key,
                        EqualsValue = null,
                        Operator = LayoutTokens.OperatorString(Op),
                        Value = Literal,
                        Inverted = false,
                    };
                case Mode.CmpKey:
                    return new VisibilityCondition
                    {
                        SaveKey = Key,
                        EqualsValue = null,
                        Operator = LayoutTokens.OperatorString(Op),
                        CompareKey = CompareKey,
                        Inverted = false,
                    };
                default:
                    throw new InvalidOperationException(
                        "When.All cannot be a single visibility condition. Use WithVisibility(When.All(...), build) so AND is nested groups.");
            }
        }

        internal string SuggestedId()
        {
            switch (Kind)
            {
                case Mode.On:
                case Mode.Off:
                    return "vis_" + Key;
                case Mode.Eq:
                case Mode.Neq:
                    return "vis_" + Key + "_" + (EqualsValue?.ToString() ?? "null");
                case Mode.CmpLiteral:
                    return "vis_" + Key + "_" + LayoutTokens.OperatorString(Op) + "_" + Literal;
                case Mode.CmpKey:
                    return "vis_" + Key + "_" + LayoutTokens.OperatorString(Op) + "_" + CompareKey;
                default:
                    return "vis_group";
            }
        }

        /// <summary>Toggle On/Off groups omit <c>indented</c> (legacy). Equals/comparator groups default to flat.</summary>
        internal bool? DefaultIndented
        {
            get
            {
                if (Kind == Mode.On || Kind == Mode.Off)
                    return null;
                if (Kind == Mode.All && Children != null && Children.Length > 0)
                    return Children[0].DefaultIndented;
                return false;
            }
        }

        private static string RequireKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("saveKey is required (non-null, non-empty).", nameof(key));
            return key.Trim();
        }
    }
}
