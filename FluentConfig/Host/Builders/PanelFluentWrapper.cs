using System;

namespace FluentConfig
{
    public class PanelFluentWrapper : FluentWrapperBase<PanelFluentWrapper, PanelBuilder>
    {
        internal PanelFluentWrapper(PanelBuilder panel, IControlOptions pending) : base(panel, pending)
        {
        }

        private PanelFluentWrapper Next() => new PanelFluentWrapper(Host, Host.GetPending());
        public PanelFluentWrapper Title(string text)
        {
            Host.FlushPending();
            Host.Title(text);
            return Next();
        }

        public PanelFluentWrapper Intro(string text)
        {
            Host.FlushPending();
            Host.Intro(text);
            return Next();
        }

        public PanelFluentWrapper Separator()
        {
            Host.FlushPending();
            Host.Separator();
            return Next();
        }

        public PanelFluentWrapper ConnectionStatus(string label, string initialStatus, string buttonText = null, Action<UiContext> onClick = null)
        {
            Host.FlushPending();
            Host.ConnectionStatus(label, initialStatus, buttonText, onClick);
            return Next();
        }

        /// <summary>
        /// Visibility group. Prefer <c>inverted: true</c> (named) or <see cref = "WithVisibilityWhenOff"/>.
        /// </summary>
        public PanelFluentWrapper WithVisibility(string toggleKey, Action<PanelBuilder> build, bool inverted = false)
        {
            Host.FlushPending();
            Host.WithVisibility(toggleKey, build, inverted);
            return Next();
        }

        public PanelFluentWrapper WithVisibility(string key, Comparator op, int value, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibility(key, op, value, build);
            return Next();
        }

        public PanelFluentWrapper WithVisibility(string key, Comparator op, string compareKey, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibility(key, op, compareKey, build);
            return Next();
        }

        public PanelFluentWrapper WithVisibilityWhenOff(string toggleKey, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.WithVisibilityWhenOff(toggleKey, build);
            return Next();
        }

        public PanelFluentWrapper WithVisibility(
            string key,
            string equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            Host.FlushPending();
            Host.WithVisibility(key, equalsValue, build, chrome);
            return Next();
        }

        public PanelFluentWrapper WithVisibility(
            string key,
            int equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            Host.FlushPending();
            Host.WithVisibility(key, equalsValue, build, chrome);
            return Next();
        }

        public PanelFluentWrapper WithVisibilityWhenNot(
            string key,
            string equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            Host.FlushPending();
            Host.WithVisibilityWhenNot(key, equalsValue, build, chrome);
            return Next();
        }

        public PanelFluentWrapper WithVisibilityWhenNot(
            string key,
            int equalsValue,
            Action<PanelBuilder> build,
            VisibilityChrome chrome = VisibilityChrome.Flat)
        {
            Host.FlushPending();
            Host.WithVisibilityWhenNot(key, equalsValue, build, chrome);
            return Next();
        }

        public PanelFluentWrapper WithRepeatableRows(string saveKey, Action<PanelBuilder> buildRow)
        {
            Host.FlushPending();
            Host.WithRepeatableRows(saveKey, buildRow);
            return Next();
        }

        public PanelFluentWrapper Grid(string spec, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.Grid(spec, build);
            return Next();
        }

        public PanelFluentWrapper Row(Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.Row(build);
            return Next();
        }

        public PanelFluentWrapper Row(string spec, Action<PanelBuilder> build)
        {
            Host.FlushPending();
            Host.Row(spec, build);
            return Next();
        }

        public PanelFluentWrapper RepeatFor(string driverKey, Action<PanelBuilder, int> build, int? max = null, int startIndex = 1)
        {
            Host.FlushPending();
            Host.RepeatFor(driverKey, build, max, startIndex);
            return Next();
        }
    }
}