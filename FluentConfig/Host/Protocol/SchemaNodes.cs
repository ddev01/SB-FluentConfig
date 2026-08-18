using System.Collections.Generic;
using Newtonsoft.Json;

namespace FluentConfig.Protocol
{
    public sealed class ToggleNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Toggle;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        /// <summary>Default for a plain on/off toggle. Ignored when <see cref="Exclusive"/> is set.</summary>
        [JsonProperty("defaultValue")]
        public bool? DefaultValue { get; set; }

        /// <summary>WithExclusive mode: at most MaxSelected options may be true.</summary>
        [JsonProperty("exclusive")]
        public ExclusiveToggleOptions Exclusive { get; set; }
    }

    public sealed class ExclusiveToggleOptions
    {
        [JsonProperty("options")]
        public IList<string> Options { get; set; }

        /// <summary>Max toggles that can be true. Min 1. Default 1 (single-select).</summary>
        [JsonProperty("maxSelected")]
        public int MaxSelected { get; set; } = 1;

        [JsonProperty("defaultIndex")]
        public int? DefaultIndex { get; set; }

        [JsonProperty("defaultIndices")]
        public IList<int> DefaultIndices { get; set; }
    }

    public sealed class TextboxNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Textbox;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [JsonProperty("password")]
        public bool Password { get; set; }

        [JsonProperty("multiline")]
        public bool Multiline { get; set; }
    }

    public sealed class SliderNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Slider;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("min")]
        public int Min { get; set; }

        [JsonProperty("max")]
        public int Max { get; set; }

        [JsonProperty("defaultValue")]
        public int? DefaultValue { get; set; }
    }

    /// <summary>
    /// Covers Input / NumberInput / IntegerInput.
    /// <see cref="ValueType"/> is "string" | "int" | "double" | "float".
    /// </summary>
    public sealed class NumberInputNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.NumberInput;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        /// <summary>"string" | "int" | "double" | "float"</summary>
        [JsonProperty("valueType")]
        public string ValueType { get; set; } = "double";

        [JsonProperty("min")]
        public double? Min { get; set; }

        [JsonProperty("max")]
        public double? Max { get; set; }

        [JsonProperty("step")]
        public double? Step { get; set; }

        [JsonProperty("defaultValue")]
        public object DefaultValue { get; set; }

        [JsonProperty("stepper")]
        public bool Stepper { get; set; }
    }

    public sealed class DropdownNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Dropdown;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        /// <summary>Display save key (or sole key when not pair-value).</summary>
        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("options")]
        public IList<DropdownOption> Options { get; set; }

        /// <summary>When set, display is stored in SaveKey and underlying value in ValueSaveKey.</summary>
        [JsonProperty("valueSaveKey")]
        public string ValueSaveKey { get; set; }

        /// <summary>When true, UI may call dropdown.refresh RPC to reload options from the host.</summary>
        [JsonProperty("refreshable")]
        public bool Refreshable { get; set; }

        [JsonProperty("defaultIndex")]
        public int? DefaultIndex { get; set; }

        [JsonProperty("defaultByValue")]
        public string DefaultByValue { get; set; }

        /// <summary>Explicit default string (AllowCustom / .Default(string)).</summary>
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [JsonProperty("searchable")]
        public bool? Searchable { get; set; }

        [JsonProperty("allowCustom")]
        public bool? AllowCustom { get; set; }

        [JsonProperty("multiple")]
        public bool? Multiple { get; set; }

        [JsonProperty("defaultValues")]
        public IList<string> DefaultValues { get; set; }
    }

    public sealed class DropdownOption
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }
    }

    public sealed class ColorPickerNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.ColorPicker;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }
    }

    public sealed class DurationInputNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.DurationInput;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        /// <summary>e.g. "30seconds", "5minutes", "permanent"</summary>
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        [JsonProperty("permanentOption")]
        public bool PermanentOption { get; set; } = true;
    }

    public sealed class FilepathNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Filepath;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        /// <summary>When true, omit the built-in Browse button (pair with adjacent action buttons).</summary>
        [JsonProperty("hideBrowse")]
        public bool HideBrowse { get; set; }

        /// <summary>When true (default), a non-empty value must exist on disk. Empty is always allowed.</summary>
        [JsonProperty("mustExist")]
        public bool MustExist { get; set; } = true;

        /// <summary>Allowed extensions including the dot (e.g. .gif, .mp3). Null = any.</summary>
        [JsonProperty("accept")]
        public IList<string> Accept { get; set; }
    }

    public sealed class DynamicTextboxesNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.DynamicTextboxes;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("preset")]
        public IList<string> Preset { get; set; }

        [JsonProperty("allowDuplicates")]
        public bool AllowDuplicates { get; set; } = true;
    }

    public sealed class ButtonNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Button;

        /// <summary>Stable id for button.click RPC (buttons have no saveKey).</summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }

    public sealed class DescriptionNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Description;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public sealed class TitleNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Title;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }

    public sealed class SeparatorNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Separator;

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    /// <summary>
    /// Extension update modal payload (also expressible as a schema node).
    /// Host pushes via <c>update.available</c>; UI never talks to GitHub.
    /// </summary>
    public sealed class UpdateNoticeNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.UpdateNotice;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("currentVersion")]
        public string CurrentVersion { get; set; }

        [JsonProperty("latestVersion")]
        public string LatestVersion { get; set; }

        [JsonProperty("releaseNotes")]
        public string ReleaseNotes { get; set; }

        /// <summary>Unused for extension notices (always empty). Kept for protocol compatibility.</summary>
        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        /// <summary>owner/name form (informational; host owns the actual API call).</summary>
        [JsonProperty("repo")]
        public string Repo { get; set; }

        [JsonProperty("dismissible")]
        public bool Dismissible { get; set; } = true;

        /// <summary>Always <c>notify</c> for in-menu extension notices.</summary>
        [JsonProperty("mode")]
        public string Mode { get; set; } = "notify";

        /// <summary>GitHub release page URL (fallback How to update).</summary>
        [JsonProperty("releasePageUrl")]
        public string ReleasePageUrl { get; set; }

        /// <summary>Preferred “How to update” URL.</summary>
        [JsonProperty("updateGuideUrl")]
        public string UpdateGuideUrl { get; set; }
    }

    /// <summary>
    /// Live connection-status indicator with optional action button (reuses button.click when ButtonId is set).
    /// </summary>
    public sealed class ConnectionStatusNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.ConnectionStatus;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        /// <summary>"connected" | "disconnected" | "connecting" | "error"</summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusText")]
        public string StatusText { get; set; }

        [JsonProperty("buttonText")]
        public string ButtonText { get; set; }

        /// <summary>When set with <see cref="ButtonText"/>, web fires button.click with this id.</summary>
        [JsonProperty("buttonId")]
        public string ButtonId { get; set; }
    }

    /// <summary>
    /// Nested schema group: visibility gate, layout container (Grid/Row), and/or RepeatFor index gate.
    /// Replaces the old Panel-builder callback boundary with pure data.
    /// </summary>
    public sealed class GroupNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.Group;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("children")]
        public IList<SchemaNode> Children { get; set; }

        /// <summary>When set, children render in a CSS grid/flex container instead of the indented stack.</summary>
        [JsonProperty("grid")]
        public GridSpec Grid { get; set; }

        /// <summary>
        /// Left-rail chrome. Null/omitted = rail (toggle gates). <c>false</c> = flat (value-equals, comparator, RepeatFor).
        /// Must serialize as <c>false</c> when flat — do not mark DefaultValueHandling.Ignore.
        /// </summary>
        [JsonProperty("indented")]
        public bool? Indented { get; set; }
    }

    /// <summary>
    /// Pill list with per-item nested sub-panels expressed as schema nodes — never UI-framework containers.
    /// Item children are SchemaNode trees (see <c>ItemTemplate</c>), not raw Panel callbacks.
    /// </summary>
    public sealed class PillInputNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.PillInput;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        /// <summary>
        /// Template of nested controls for each pill. Relative saveKeys may use "{name}" as a
        /// placeholder expanded to the pill item name (e.g. "{name}_enabled").
        /// </summary>
        [JsonProperty("itemTemplate")]
        public IList<SchemaNode> ItemTemplate { get; set; }

        /// <summary>
        /// Host-expanded per-item schemas for pills that already exist at bootstrap
        /// (or after pill.changed RPC). When present, preferred over expanding ItemTemplate client-side.
        /// </summary>
        [JsonProperty("items")]
        public IList<PillItemSchema> Items { get; set; }
    }

    public sealed class PillItemSchema
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("children")]
        public IList<SchemaNode> Children { get; set; }
    }

    /// <summary>
    /// Repeatable row list. Each row is an object under saveKey[i]; rowSchema uses relative keys.
    /// </summary>
    public sealed class RepeatableRowsNode : SchemaNode
    {
        public override string Type => SchemaNodeTypes.RepeatableRows;

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("saveKey")]
        public string SaveKey { get; set; }

        [JsonProperty("rowSchema")]
        public IList<SchemaNode> RowSchema { get; set; }
    }
}
