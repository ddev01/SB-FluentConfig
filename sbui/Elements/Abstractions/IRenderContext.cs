using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Sbui.Core;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Sbui.Elements
{
    /// <summary>
    /// Context passed to UIElement.Render; provides panel, settings, registry, factory, and callbacks.
    /// </summary>
    public interface IRenderContext
    {
        /// <summary>Gets the panel to add content to (tab panel or visibility override).</summary>
        StackPanel GetPanel(string tabName);
        /// <summary>Sets the panel used for visibility-conditioned groups so GetPanel returns it until cleared.</summary>
        void SetVisibilityOverridePanel(StackPanel panel);
        /// <summary>Clears the visibility override panel.</summary>
        void ClearVisibilityOverridePanel();
        JObject Settings { get; }
        /// <summary>Gets a setting by path (supports nested keys like "voice_aliases[0].name").</summary>
        JToken GetSetting(string path);
        ControlRegistry Registry { get; }
        void MarkDirty();
        FluentWindow Window { get; }
        void Log(string message);
        void UpdateDropdown(string key, string[] options, int selectedIndex);
        void UpdateDropdownWithPairValue(string displayKey, string valueKey, System.Collections.Generic.IEnumerable<(string Value, string Display)> options, string selectedValue);
        /// <summary>For dynamic textbox lists; key -> panel that contains the list of textboxes.</summary>
        IDictionary<string, StackPanel> DynamicTextboxPanels { get; }
    }
}
