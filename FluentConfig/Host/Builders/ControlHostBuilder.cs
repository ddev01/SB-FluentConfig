using System;

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

        internal void FlushPending() => Pending?.Flush();

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
    }

    public abstract class ControlHostBuilder<TWrapper> : ControlHostBuilder
    {
        protected ControlHostBuilder(FluentConfigSession session, SchemaNodeList nodes)
            : base(session, nodes) { }

        protected abstract TWrapper Wrap();

        private TWrapper Begin(Action<PendingControl> start)
        {
            FlushPending();
            Pending = new PendingControl(Session, Nodes);
            start(Pending);
            return Wrap();
        }

        public TWrapper Toggle(string label, string key) => Begin(p => p.BeginToggle(label, key));
        public TWrapper Textbox(string label, string key) => Begin(p => p.BeginTextbox(label, key));
        public TWrapper Slider(string label, string key) => Begin(p => p.BeginSlider(label, key));
        public TWrapper Button(string label) => Begin(p => p.BeginButton(label));
        public TWrapper Input(string label, string key) => Begin(p => p.BeginInput(label, key));
        public TWrapper IntegerInput(string label, string key) => Begin(p => p.BeginIntegerInput(label, key));
        public TWrapper DurationInput(string label, string key) => Begin(p => p.BeginDurationInput(label, key));
        public TWrapper Filepath(string label, string key) => Begin(p => p.BeginFilepath(label, key));
        public TWrapper NumberInput(string label, string key) => Begin(p => p.BeginNumberInput(label, key));
        public TWrapper ColorPicker(string label, string key) => Begin(p => p.BeginColorPicker(label, key));
        public TWrapper Dropdown(string label, string key) => Begin(p => p.BeginDropdown(label, key));
        public TWrapper DynamicTextboxes(string label, string key) => Begin(p => p.BeginDynamicTextboxes(label, key));
        public TWrapper PillInput(string label, string key) => Begin(p => p.BeginPillInput(label, key));
    }
}
