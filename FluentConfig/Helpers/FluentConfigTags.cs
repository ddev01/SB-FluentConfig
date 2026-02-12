namespace FluentConfig.Helpers
{
    /// <summary>
    /// Tag prefixes used when setting control/element Tag for settings sync and registry lookup.
    /// </summary>
    public static class FluentConfigTags
    {
        public const string IntegerPrefix = "integer:";
        public const string DoublePrefix = "double:";
        public const string FloatPrefix = "float:";
        public const string CompetingPrefix = "competing:";
        public const string DynamicPrefix = "dynamic:";
        public const string PillPrefix = "pill:";
        public const string DurationPrefix = "duration:";
        public const string PairPrefix = "pair:";
        public const string ColorPrefix = "color:";

        private static readonly string[] KnownPrefixes =
        {
            IntegerPrefix, DoublePrefix, FloatPrefix, ColorPrefix,
            CompetingPrefix, DynamicPrefix, PillPrefix, DurationPrefix, PairPrefix
        };

        /// <summary>
        /// Strips any known tag prefix and returns the real settings key.
        /// For pair tags ("pair:displayKey,valueKey") returns "displayKey,valueKey".
        /// Returns the tag itself if no known prefix is found.
        /// Returns null if tag is null or empty.
        /// </summary>
        public static string StripPrefix(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return null;
            foreach (var prefix in KnownPrefixes)
            {
                if (tag.StartsWith(prefix))
                    return tag.Substring(prefix.Length);
            }
            return tag;
        }
    }
}
