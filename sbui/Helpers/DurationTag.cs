namespace Sbui.Helpers
{
    /// <summary>
    /// Tag for duration control with custom unit labels. When set, ShortCodes are used for persistence (e.g. "s","m","h","d") instead of combo display text.
    /// </summary>
    public sealed class DurationTag
    {
        public string Key { get; }
        public string[] ShortCodes { get; }

        public DurationTag(string key, string[] shortCodes)
        {
            Key = key ?? "";
            ShortCodes = shortCodes;
        }
    }
}
