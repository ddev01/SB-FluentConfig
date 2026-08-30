using System;
using System.Collections.Generic;

namespace FluentConfig
{
    /// <summary>
    /// Host for control-creation methods that flush into a schema node list.
    /// </summary>
    public abstract class ControlHostBuilder
    {
        protected readonly FluentConfigSession Session;
        protected readonly SchemaNodeList Nodes;
        protected PendingControl Pending;

        protected ControlHostBuilder(FluentConfigSession session, SchemaNodeList nodes)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        }

        internal SchemaNodeList TargetNodes => Nodes;

        internal void FlushPending()
        {
            if (Pending == null)
                return;
            Pending.Flush();
            Pending = null;
        }

        internal PendingControl GetPending() => Pending;

        protected void AddIntro(string text)
        {
            FlushPending();
            Nodes.Add(new Protocol.DescriptionNode { Text = text ?? "" });
        }

        protected void AddTitle(string text)
        {
            FlushPending();
            Nodes.Add(new Protocol.TitleNode { Text = text ?? "" });
        }

        protected void AddSeparator()
        {
            FlushPending();
            Nodes.Add(new Protocol.SeparatorNode());
        }

        protected void AddConnectionStatus(
            string label,
            string initialStatus,
            string buttonText,
            Action<UiContext> onClick)
        {
            FlushPending();
            // Stable id from label so authors can PatchSchema without an extra id parameter.
            var id = SchemaBuilderHelpers.UniqueId(Session, "conn_" + (label ?? "status").Replace(' ', '_'));
            string buttonId = null;
            if (!string.IsNullOrEmpty(buttonText))
            {
                buttonId = SchemaBuilderHelpers.UniqueId(Session, "btn_" + id);
                if (onClick != null)
                    Session.RegisterButtonClick(buttonId, onClick);
            }

            Nodes.Add(new Protocol.ConnectionStatusNode
            {
                Id = id,
                Label = label ?? "",
                Status = string.IsNullOrEmpty(initialStatus) ? "disconnected" : initialStatus,
                ButtonText = buttonText,
                ButtonId = buttonId,
            });
        }
    }

    /// <summary>
    /// CRTP host: <see cref="Toggle"/> and option chains return the concrete
    /// <see cref="SectionBuilder"/> / <see cref="PanelBuilder"/> (no wrapper types).
    /// </summary>
    public abstract class ControlHostBuilder<TSelf> : ControlHostBuilder
        where TSelf : ControlHostBuilder<TSelf>
    {
        protected ControlHostBuilder(FluentConfigSession session, SchemaNodeList nodes)
            : base(session, nodes) { }

        private TSelf Self => (TSelf)this;

        private TSelf Begin(Action<PendingControl> start)
        {
            FlushPending();
            Pending = new PendingControl(Session, Nodes);
            start(Pending);
            return Self;
        }

        private TSelf Option(Action<IControlOptions> apply)
        {
            if (Pending == null)
            {
                throw new InvalidOperationException(
                    "No pending control to apply options to. Call a control method (e.g. .Toggle) before option methods, or avoid chaining options after structural calls like Intro/Separator.");
            }

            apply?.Invoke(Pending);
            return Self;
        }

        public TSelf Hint(string text) => Option(o => o.Hint(text));
        public TSelf Default(bool value) => Option(o => o.Default(value));
        public TSelf Default(string value) => Option(o => o.Default(value));
        public TSelf Default(string[] values) => Option(o => o.Default(values));
        public TSelf Default(int value) => Option(o => o.Default(value));
        public TSelf Default(double value) => Option(o => o.Default(value));
        public TSelf Range(int min, int max) => Option(o => o.Range(min, max));
        public TSelf Range(double min, double max) => Option(o => o.Range(min, max));
        public TSelf Step(double value) => Option(o => o.Step(value));
        public TSelf Password() => Option(o => o.Password());
        public TSelf Multiline() => Option(o => o.Multiline());
        public TSelf ShowWhen(string key) => Option(o => o.ShowWhen(key));
        public TSelf ShowWhen(string key, string equalsValue) => Option(o => o.ShowWhen(key, equalsValue));
        public TSelf ShowWhen(string key, int equalsValue) => Option(o => o.ShowWhen(key, equalsValue));
        public TSelf ShowWhenNot(string key, string equalsValue) => Option(o => o.ShowWhenNot(key, equalsValue));
        public TSelf ShowWhenNot(string key, int equalsValue) => Option(o => o.ShowWhenNot(key, equalsValue));
        public TSelf ShowWhen(string key, Comparator op, int value) => Option(o => o.ShowWhen(key, op, value));
        public TSelf ShowWhen(string key, Comparator op, string compareKey) => Option(o => o.ShowWhen(key, op, compareKey));
        public TSelf ShowWhen(When condition) => Option(o => o.ShowWhen(condition));
        public TSelf Size(string spec) => Option(o => o.Size(spec));
        public TSelf Options(string[] options) => Option(o => o.Options(options));
        public TSelf Options(IEnumerable<(string Value, string Display)> pairOptions) => Option(o => o.OptionsPairs(pairOptions));
        public TSelf WithPairValue(string valueKey) => Option(o => o.WithPairValue(valueKey));
        public TSelf DefaultByValue(string value) => Option(o => o.DefaultByValue(value));
        public TSelf DefaultIndex(int index) => Option(o => o.DefaultIndex(index));
        public TSelf WithExclusive(string[] options) => Option(o => o.WithExclusive(options));
        public TSelf MaxSelected(int n) => Option(o => o.MaxSelected(n));
        public TSelf DefaultIndices(int[] indices) => Option(o => o.DefaultIndices(indices));
        public TSelf Refresh(Func<string[]> callback) => Option(o => o.Refresh(callback));
        public TSelf Refresh(Func<IEnumerable<(string Value, string Display)>> callback) => Option(o => o.RefreshPairs(callback));
        public TSelf Searchable() => Option(o => o.Searchable());
        public TSelf AllowCustom() => Option(o => o.AllowCustom());
        public TSelf Multiple() => Option(o => o.Multiple());
        public TSelf Preset(string[] values) => Option(o => o.Preset(values));
        public TSelf AllowDuplicates(bool value = true) => Option(o => o.AllowDuplicates(value));
        public TSelf Color(string hex) => Option(o => o.Color(hex));
        public TSelf Text(string caption) => Option(o => o.Text(caption));
        public TSelf OnClick(Action<UiContext> callback) => Option(o => o.OnClick(callback));
        /// <summary>Filepath only: omit the built-in Browse button (use your own Local / Giphy actions).</summary>
        public TSelf HideBrowse() => Option(o => o.HideBrowse());
        /// <summary>Filepath only: non-empty values must exist on disk (default true).</summary>
        public TSelf MustExist(bool value = true) => Option(o => o.MustExist(value));
        /// <summary>Filepath only: allowed extensions (gif, mp3, …).</summary>
        public TSelf Accept(params string[] extensions) => Option(o => o.Accept(extensions));
        public TSelf WithPermanentOption(bool value) => Option(o => o.WithPermanentOption(value));
        public TSelf WithStepper(bool value = true) => Option(o => o.WithStepper(value));
        /// <summary>Nested pill item schema via fluent sub-builder.</summary>
        public TSelf ItemTemplate(Action<PanelBuilder> build) => Option(o => o.ItemTemplate(build));
        /// <summary>Alias for <see cref="ItemTemplate"/>.</summary>
        public TSelf WithItemTemplate(Action<PanelBuilder> build) => ItemTemplate(build);
        public TSelf OnPillAdded(Action<string, CallbackContext> callback) => Option(o => o.OnPillAdded(callback));
        public TSelf OnPillRemoved(Action<string, CallbackContext> callback) => Option(o => o.OnPillRemoved(callback));

        public TSelf Toggle(string label, string key) => Begin(p => p.BeginToggle(label, key));
        public TSelf Textbox(string label, string key) => Begin(p => p.BeginTextbox(label, key));
        public TSelf Slider(string label, string key) => Begin(p => p.BeginSlider(label, key));
        public TSelf Button(string label) => Begin(p => p.BeginButton(label));
        public TSelf IntegerInput(string label, string key) => Begin(p => p.BeginIntegerInput(label, key));
        public TSelf DurationInput(string label, string key) => Begin(p => p.BeginDurationInput(label, key));
        public TSelf Filepath(string label, string key) => Begin(p => p.BeginFilepath(label, key));
        public TSelf NumberInput(string label, string key) => Begin(p => p.BeginNumberInput(label, key));
        public TSelf ColorPicker(string label, string key) => Begin(p => p.BeginColorPicker(label, key));
        public TSelf Dropdown(string label, string key) => Begin(p => p.BeginDropdown(label, key));
        public TSelf Combobox(string label, string key) => Begin(p => p.BeginCombobox(label, key));
        public TSelf DynamicTextboxes(string label, string key) => Begin(p => p.BeginDynamicTextboxes(label, key));
        public TSelf PillInput(string label, string key) => Begin(p => p.BeginPillInput(label, key));

        public TSelf Intro(string text)
        {
            AddIntro(text);
            return Self;
        }

        public TSelf Title(string text)
        {
            AddTitle(text);
            return Self;
        }

        public TSelf Separator()
        {
            AddSeparator();
            return Self;
        }

        /// <summary>
        /// Connection-status indicator. Optional action button reuses the existing button.click RPC.
        /// Live updates: push a replacement node via <see cref="UiContext.PatchSchemaNode"/>.
        /// </summary>
        public TSelf ConnectionStatus(string label, string initialStatus, string buttonText = null, Action<UiContext> onClick = null)
        {
            AddConnectionStatus(label, initialStatus, buttonText, onClick);
            return Self;
        }

        public TSelf WithVisibility(When condition, Action<PanelBuilder> build) =>
            WithVisibility(condition, build, chrome: null);

        public TSelf WithVisibility(When condition, Action<PanelBuilder> build, VisibilityChrome chrome) =>
            WithVisibility(condition, build, (VisibilityChrome?)chrome);

        private TSelf WithVisibility(When condition, Action<PanelBuilder> build, VisibilityChrome? chrome)
        {
            SchemaBuilderHelpers.AddWhenVisibility(Session, Nodes, FlushPending, condition, build, chrome);
            return Self;
        }

        /// <summary>Group visible when the toggle is on. For off, use <see cref="WithVisibilityWhenOff"/> or <c>When.Off</c>.</summary>
        public TSelf WithVisibility(string toggleKey, Action<PanelBuilder> build) =>
            WithVisibility(When.On(toggleKey), build);

        /// <summary>Group gated by a numeric comparator against a literal value.</summary>
        public TSelf WithVisibility(string key, Comparator op, int value, Action<PanelBuilder> build) =>
            WithVisibility(When.Cmp(key, op, value), build);

        /// <summary>Group gated by a numeric comparator against another live saveKey.</summary>
        public TSelf WithVisibility(string key, Comparator op, string compareKey, Action<PanelBuilder> build) =>
            WithVisibility(When.Cmp(key, op, compareKey), build);

        /// <summary>Group visible only when the toggle is off.</summary>
        public TSelf WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build) =>
            WithVisibility(When.Off(toggleKey), build);

        /// <summary>Group visible when <paramref name="key"/> equals a saved string (dropdown, etc.).</summary>
        public TSelf WithVisibility(
            string key,
            string equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat) =>
            WithVisibility(When.Eq(key, equalsValue), build, chrome);

        /// <summary>Group visible when <paramref name="key"/> equals a numeric saved value.</summary>
        public TSelf WithVisibility(
            string key,
            int equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat) =>
            WithVisibility(When.Eq(key, equalsValue), build, chrome);

        /// <summary>Group visible when <paramref name="key"/> does not equal the string.</summary>
        public TSelf WithVisibilityWhenNot(
            string key,
            string equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat) =>
            WithVisibility(When.Neq(key, equalsValue), build, chrome);

        /// <summary>Group visible when <paramref name="key"/> does not equal the number.</summary>
        public TSelf WithVisibilityWhenNot(
            string key,
            int equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat) =>
            WithVisibility(When.Neq(key, equalsValue), build, chrome);

        /// <summary>Adds a repeatable-rows control with a relative row schema.</summary>
        public TSelf WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            SchemaBuilderHelpers.AddRepeatableRows(Session, Nodes, FlushPending, saveKey, buildRow);
            return Self;
        }

        /// <summary>CSS grid container. Spec tokens: <c>grid-cols-N</c>, <c>gap-N</c> (space-separated).</summary>
        public TSelf Grid(string spec, Action<PanelBuilder> build)
        {
            SchemaBuilderHelpers.AddLayoutGroup(Session, Nodes, FlushPending, LayoutTokens.ParseGridSpec(spec), build);
            return Self;
        }

        /// <summary>Flex-wrap row container (default gap 3).</summary>
        public TSelf Row(Action<PanelBuilder> build) => Row("", build);

        /// <summary>Flex-wrap row. Spec tokens: <c>gap-N</c>, <c>items-start|center|end|stretch|baseline</c>.</summary>
        public TSelf Row(string spec, Action<PanelBuilder> build)
        {
            SchemaBuilderHelpers.AddLayoutGroup(Session, Nodes, FlushPending, LayoutTokens.ParseRowSpec(spec), build);
            return Self;
        }

        /// <summary>
        /// Materializes controls for indices <paramref name="startIndex"/>..<paramref name="max"/>,
        /// gating indices above the driver's declared min with <c>ShowWhen(driver, gte, i)</c>.
        /// When <paramref name="max"/> is omitted, uses the driver's declared <c>.Range(...)</c>.
        /// </summary>
        public TSelf RepeatFor(string driverKey, Action<PanelBuilder, int> build, int? max = null, int startIndex = 1)
        {
            SchemaBuilderHelpers.AddRepeatFor(Session, Nodes, FlushPending, driverKey, build, max, startIndex);
            return Self;
        }
    }
}
