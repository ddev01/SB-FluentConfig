using System;
using System.Collections.Generic;
using FluentConfig.Protocol;

namespace FluentConfig
{
    internal static class SchemaValidation
    {
        internal static void AssertUniqueSaveKeys(IList<SectionSchema> sections)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (sections == null) return;
            foreach (var section in sections)
                WalkNodes(section?.Children, keys, documentScope: true);
        }

        private static void WalkNodes(IList<SchemaNode> nodes, HashSet<string> keys, bool documentScope)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node == null) continue;
                switch (node)
                {
                    case ToggleNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case TextboxNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case SliderNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case NumberInputNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case DurationInputNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case FilepathNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case ColorPickerNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case DynamicTextboxesNode n:
                        Reserve(keys, n.SaveKey);
                        break;
                    case DropdownNode n:
                        Reserve(keys, n.SaveKey);
                        Reserve(keys, n.ValueSaveKey);
                        break;
                    case PillInputNode n:
                        Reserve(keys, n.SaveKey);
                        WalkNodes(n.ItemTemplate, new HashSet<string>(StringComparer.Ordinal), documentScope: false);
                        if (n.Items != null)
                        {
                            foreach (var item in n.Items)
                                WalkNodes(item?.Children, keys, documentScope: true);
                        }
                        break;
                    case RepeatableRowsNode n:
                        Reserve(keys, n.SaveKey);
                        WalkNodes(n.RowSchema, new HashSet<string>(StringComparer.Ordinal), documentScope: false);
                        break;
                    case GroupNode n:
                        WalkNodes(n.Children, keys, documentScope);
                        break;
                }
            }
        }

        private static void Reserve(HashSet<string> keys, string saveKey)
        {
            if (string.IsNullOrEmpty(saveKey))
                return;
            if (!keys.Add(saveKey))
                throw new InvalidOperationException(
                    $"Duplicate saveKey '{saveKey}'. Each control (including pair-value dropdowns) must use a unique key.");
        }
    }
}
