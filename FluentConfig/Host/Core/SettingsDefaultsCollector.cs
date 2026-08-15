using System;
using System.Collections.Generic;
using FluentConfig.Protocol;
using Newtonsoft.Json.Linq;

namespace FluentConfig.Core
{
    /// <summary>
    /// Walks a built schema tree and collects explicit control defaults as saveKey → token.
    /// Mirrors web control display fallbacks (defaultValue / defaultIndex / preset / exclusive indices).
    /// </summary>
    internal static class SettingsDefaultsCollector
    {
        public static Dictionary<string, JToken> Collect(IList<SectionSchema> sections, JObject currentValues)
        {
            var result = new Dictionary<string, JToken>(StringComparer.Ordinal);
            if (sections == null) return result;
            foreach (var section in sections)
                WalkNodes(section?.Children, result, currentValues);
            return result;
        }

        private static void WalkNodes(
            IList<SchemaNode> nodes,
            Dictionary<string, JToken> result,
            JObject currentValues)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node == null) continue;
                switch (node)
                {
                    case ToggleNode toggle:
                        CollectToggle(toggle, result);
                        break;
                    case TextboxNode textbox when HasKey(textbox.SaveKey) && textbox.DefaultValue != null:
                        Put(result, textbox.SaveKey, textbox.DefaultValue);
                        break;
                    case SliderNode slider when HasKey(slider.SaveKey) && slider.DefaultValue.HasValue:
                        Put(result, slider.SaveKey, slider.DefaultValue.Value);
                        break;
                    case NumberInputNode number when HasKey(number.SaveKey) && number.DefaultValue != null:
                        Put(result, number.SaveKey, JToken.FromObject(number.DefaultValue));
                        break;
                    case DropdownNode dropdown:
                        CollectDropdown(dropdown, result);
                        break;
                    case ColorPickerNode color when HasKey(color.SaveKey) && color.DefaultValue != null:
                        Put(result, color.SaveKey, color.DefaultValue);
                        break;
                    case DurationInputNode duration when HasKey(duration.SaveKey) && duration.DefaultValue != null:
                        Put(result, duration.SaveKey, duration.DefaultValue);
                        break;
                    case FilepathNode filepath when HasKey(filepath.SaveKey) && filepath.DefaultValue != null:
                        Put(result, filepath.SaveKey, filepath.DefaultValue);
                        break;
                    case DynamicTextboxesNode dynamic when HasKey(dynamic.SaveKey) && dynamic.Preset != null:
                        Put(result, dynamic.SaveKey, JArray.FromObject(dynamic.Preset));
                        break;
                    case GroupNode group:
                        WalkNodes(group.Children, result, currentValues);
                        break;
                    case PillInputNode pill:
                        CollectPill(pill, result, currentValues);
                        break;
                    case RepeatableRowsNode rows:
                        CollectRepeatableRows(rows, result, currentValues);
                        break;
                }
            }
        }

        private static void CollectToggle(ToggleNode toggle, Dictionary<string, JToken> result)
        {
            if (!HasKey(toggle.SaveKey)) return;

            if (toggle.Exclusive != null)
            {
                IList<int> indices = null;
                if (toggle.Exclusive.DefaultIndices != null && toggle.Exclusive.DefaultIndices.Count > 0)
                    indices = toggle.Exclusive.DefaultIndices;
                else if (toggle.Exclusive.DefaultIndex.HasValue)
                    indices = new[] { toggle.Exclusive.DefaultIndex.Value };

                if (indices != null)
                    Put(result, toggle.SaveKey, JArray.FromObject(indices));
                return;
            }

            if (toggle.DefaultValue.HasValue)
                Put(result, toggle.SaveKey, toggle.DefaultValue.Value);
        }

        private static void CollectDropdown(DropdownNode dropdown, Dictionary<string, JToken> result)
        {
            if (!HasKey(dropdown.SaveKey)) return;

            if (dropdown.Multiple == true)
            {
                if (dropdown.DefaultValues != null)
                    Put(result, dropdown.SaveKey, JArray.FromObject(dropdown.DefaultValues));
                else
                    Put(result, dropdown.SaveKey, new JArray());
                return;
            }

            var options = dropdown.Options;
            if (options == null || options.Count == 0)
            {
                if (!string.IsNullOrEmpty(dropdown.DefaultValue))
                    Put(result, dropdown.SaveKey, dropdown.DefaultValue);
                return;
            }

            DropdownOption opt = null;
            if (!string.IsNullOrEmpty(dropdown.DefaultByValue))
            {
                foreach (var o in options)
                {
                    if (o != null && string.Equals(o.Value, dropdown.DefaultByValue, StringComparison.Ordinal))
                    {
                        opt = o;
                        break;
                    }
                }
            }
            else if (dropdown.DefaultIndex.HasValue)
            {
                var i = dropdown.DefaultIndex.Value;
                if (i >= 0 && i < options.Count)
                    opt = options[i];
            }

            if (opt == null && !string.IsNullOrEmpty(dropdown.DefaultValue))
            {
                Put(result, dropdown.SaveKey, dropdown.DefaultValue);
                return;
            }

            if (opt == null && options.Count > 0)
                opt = options[0];

            if (opt == null) return;

            if (!string.IsNullOrEmpty(dropdown.ValueSaveKey))
            {
                Put(result, dropdown.SaveKey, opt.Display ?? opt.Value ?? "");
                Put(result, dropdown.ValueSaveKey, opt.Value ?? "");
            }
            else
            {
                Put(result, dropdown.SaveKey, opt.Value ?? opt.Display ?? "");
            }
        }

        private static void CollectPill(
            PillInputNode pill,
            Dictionary<string, JToken> result,
            JObject currentValues)
        {
            // Nested template defaults apply only for pills that already exist.
            if (pill.Items == null) return;
            foreach (var item in pill.Items)
            {
                if (item?.Children == null) continue;
                WalkNodes(item.Children, result, currentValues);
            }
        }

        private static void CollectRepeatableRows(
            RepeatableRowsNode rows,
            Dictionary<string, JToken> result,
            JObject currentValues)
        {
            if (!HasKey(rows.SaveKey) || rows.RowSchema == null || rows.RowSchema.Count == 0)
                return;

            var existing = currentValues != null
                ? SettingsPathHelper.GetNestedValue(currentValues, rows.SaveKey) as JArray
                : null;
            if (existing == null || existing.Count == 0)
                return;

            for (int i = 0; i < existing.Count; i++)
            {
                var prefix = rows.SaveKey + "[" + i + "]";
                foreach (var child in rows.RowSchema)
                    CollectRowFieldDefault(child, prefix, result);
            }
        }

        private static void CollectRowFieldDefault(
            SchemaNode node,
            string pathPrefix,
            Dictionary<string, JToken> result)
        {
            if (node == null) return;
            switch (node)
            {
                case TextboxNode t when HasKey(t.SaveKey) && t.DefaultValue != null:
                    Put(result, pathPrefix + "." + t.SaveKey, t.DefaultValue);
                    break;
                case SliderNode s when HasKey(s.SaveKey) && s.DefaultValue.HasValue:
                    Put(result, pathPrefix + "." + s.SaveKey, s.DefaultValue.Value);
                    break;
                case NumberInputNode n when HasKey(n.SaveKey) && n.DefaultValue != null:
                    Put(result, pathPrefix + "." + n.SaveKey, JToken.FromObject(n.DefaultValue));
                    break;
                case ToggleNode tog when HasKey(tog.SaveKey) && tog.Exclusive == null && tog.DefaultValue.HasValue:
                    Put(result, pathPrefix + "." + tog.SaveKey, tog.DefaultValue.Value);
                    break;
            }
        }

        private static bool HasKey(string saveKey) => !string.IsNullOrEmpty(saveKey);

        private static void Put(Dictionary<string, JToken> result, string key, object value)
        {
            if (string.IsNullOrEmpty(key) || value == null) return;
            if (result.ContainsKey(key)) return;
            result[key] = value is JToken token ? token.DeepClone() : JToken.FromObject(value);
        }
    }
}
