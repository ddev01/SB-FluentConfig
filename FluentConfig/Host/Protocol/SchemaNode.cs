using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace FluentConfig.Protocol
{
    /*
     * Discriminated-union approach (chosen over a Type + properties-bag shape):
     * Each control is a concrete subclass of SchemaNode with a fixed "type" discriminator.
     * Reasons: (1) the Svelte renderer switches on node.type and needs typed fields;
     * (2) C# builders emit strongly typed nodes without casting through Dictionary/JObject;
     * (3) a bag would force both workstreams to guess at key names and lose compile-time checks —
     * the whole point of this Phase 0 contract. Polymorphic JSON uses SchemaNodeConverter.
     */

    /// <summary>Base schema node. Wire discriminator property: <c>type</c>.</summary>
    public abstract class SchemaNode
    {
        [JsonProperty("type")]
        public abstract string Type { get; }

        /// <summary>Optional ShowWhen-style gate on this node.</summary>
        [JsonProperty("visibility")]
        public VisibilityCondition Visibility { get; set; }
    }

    /// <summary>
    /// Reads/writes SchemaNode subclasses via the <c>type</c> discriminator.
    /// Nested children (group / pill / repeatable-rows) recurse through this converter safely.
    /// </summary>
    public sealed class SchemaNodeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) =>
            typeof(SchemaNode).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            // Serialize by concrete contract properties; nested SchemaNode values re-enter this converter.
            var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(value.GetType());
            writer.WriteStartObject();
            foreach (var prop in contract.Properties)
            {
                if (prop.Ignored || !prop.Readable)
                    continue;
                if (prop.ShouldSerialize != null && !prop.ShouldSerialize(value))
                    continue;

                var propValue = prop.ValueProvider.GetValue(value);
                if (propValue == null && serializer.NullValueHandling == NullValueHandling.Ignore)
                    continue;

                writer.WritePropertyName(prop.PropertyName);
                serializer.Serialize(writer, propValue);
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var jo = JObject.Load(reader);
            var type = jo["type"]?.Value<string>();
            if (string.IsNullOrEmpty(type))
                throw new JsonSerializationException("SchemaNode missing required 'type' discriminator.");

            var target = CreateNode(type);
            using (var subReader = jo.CreateReader())
                serializer.Populate(subReader, target);
            return target;
        }

        private static SchemaNode CreateNode(string type)
        {
            switch (type)
            {
                case SchemaNodeTypes.Toggle: return new ToggleNode();
                case SchemaNodeTypes.Textbox: return new TextboxNode();
                case SchemaNodeTypes.Slider: return new SliderNode();
                case SchemaNodeTypes.NumberInput: return new NumberInputNode();
                case SchemaNodeTypes.Dropdown: return new DropdownNode();
                case SchemaNodeTypes.ColorPicker: return new ColorPickerNode();
                case SchemaNodeTypes.DurationInput: return new DurationInputNode();
                case SchemaNodeTypes.Filepath: return new FilepathNode();
                case SchemaNodeTypes.PillInput: return new PillInputNode();
                case SchemaNodeTypes.DynamicTextboxes: return new DynamicTextboxesNode();
                case SchemaNodeTypes.RepeatableRows: return new RepeatableRowsNode();
                case SchemaNodeTypes.Button: return new ButtonNode();
                case SchemaNodeTypes.Description: return new DescriptionNode();
                case SchemaNodeTypes.Title: return new TitleNode();
                case SchemaNodeTypes.Separator: return new SeparatorNode();
                case SchemaNodeTypes.UpdateNotice: return new UpdateNoticeNode();
                case SchemaNodeTypes.Group: return new GroupNode();
                default:
                    throw new JsonSerializationException($"Unknown schema node type '{type}'.");
            }
        }
    }

    /// <summary>Canonical <c>type</c> discriminator string values (wire format).</summary>
    public static class SchemaNodeTypes
    {
        public const string Toggle = "toggle";
        public const string Textbox = "textbox";
        public const string Slider = "slider";
        public const string NumberInput = "number-input";
        public const string Dropdown = "dropdown";
        public const string ColorPicker = "color-picker";
        public const string DurationInput = "duration-input";
        public const string Filepath = "filepath";
        public const string PillInput = "pill-input";
        public const string DynamicTextboxes = "dynamic-textboxes";
        public const string RepeatableRows = "repeatable-rows";
        public const string Button = "button";
        public const string Description = "description";
        public const string Title = "title";
        public const string Separator = "separator";
        public const string UpdateNotice = "update-notice";
        public const string Group = "group";
    }
}
