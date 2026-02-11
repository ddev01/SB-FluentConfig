namespace Sbui.Helpers
{
    /// <summary>
    /// Tag for a ComboBox used as a pair-value dropdown (display + id). Stored on ComboBox.Tag for SettingsSynchronizer and ControlRegistry.
    /// </summary>
    public sealed class PairDropdownTag
    {
        public string DisplayKey { get; }
        public string IdKey { get; }

        public PairDropdownTag(string displayKey, string idKey)
        {
            DisplayKey = displayKey ?? "";
            IdKey = idKey ?? "";
        }
    }
}
