using System;
using System.Windows;
using System.Windows.Controls;
using FluentConfig.Components;
using FluentConfig.Helpers;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace FluentConfig.Elements
{
    /// <summary>
    /// Options for constructing an InputElement, replacing the 17-parameter constructor.
    /// </summary>
    public class InputElementOptions
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string TabName { get; set; } = "";
        public string SaveKey { get; set; }
        public InputValidation.InputType InputType { get; set; }
        public string DefaultString { get; set; } = "";
        public int DefaultInt { get; set; }
        public double DefaultDouble { get; set; }
        public float DefaultFloat { get; set; }
        public int MinInt { get; set; }
        public int MaxInt { get; set; } = int.MaxValue;
        public double MinDouble { get; set; }
        public double MaxDouble { get; set; } = 100;
        public float MinFloat { get; set; }
        public float MaxFloat { get; set; } = 100;
        public double Step { get; set; } = 1;
        public bool WithStepper { get; set; }
        public string Width { get; set; }
        public string VisibilityKey { get; set; }
    }

    public class InputElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public InputValidation.InputType InputType { get; set; }
        public string DefaultString { get; set; }
        public int DefaultInt { get; set; }
        public double DefaultDouble { get; set; }
        public float DefaultFloat { get; set; }
        public int MinInt { get; set; }
        public int MaxInt { get; set; }
        public double MinDouble { get; set; }
        public double MaxDouble { get; set; }
        public float MinFloat { get; set; }
        public float MaxFloat { get; set; }
        public double Step { get; set; }
        public bool WithStepper { get; set; }
        public string WidthMode { get; set; }

        /// <summary>
        /// Construct from an options object (preferred).
        /// </summary>
        public InputElement(InputElementOptions opts)
        {
            if (opts == null) throw new ArgumentNullException(nameof(opts));
            Title = opts.Title ?? "";
            Description = opts.Description ?? "";
            TabName = opts.TabName ?? "";
            SaveKey = opts.SaveKey;
            InputType = opts.InputType;
            DefaultString = opts.DefaultString ?? "";
            DefaultInt = opts.DefaultInt;
            DefaultDouble = opts.DefaultDouble;
            DefaultFloat = opts.DefaultFloat;
            MinInt = opts.MinInt;
            MaxInt = opts.MaxInt;
            MinDouble = opts.MinDouble;
            MaxDouble = opts.MaxDouble;
            MinFloat = opts.MinFloat;
            MaxFloat = opts.MaxFloat;
            Step = opts.Step;
            WithStepper = opts.WithStepper && (opts.InputType == InputValidation.InputType.Double || opts.InputType == InputValidation.InputType.Float);
            WidthMode = opts.Width;
            VisibilityKey = opts.VisibilityKey;
        }

        /// <summary>
        /// Legacy positional constructor — delegates to the options constructor.
        /// </summary>
        public InputElement(
            string title, string description, string tabName, string saveKey,
            InputValidation.InputType inputType,
            string defaultString, int defaultInt, double defaultDouble, float defaultFloat,
            int minInt, int maxInt, double minDouble, double maxDouble, float minFloat, float maxFloat,
            double step, bool withStepper,
            string visibilityKey = null)
            : this(new InputElementOptions
            {
                Title = title,
                Description = description,
                TabName = tabName,
                SaveKey = saveKey,
                InputType = inputType,
                DefaultString = defaultString,
                DefaultInt = defaultInt,
                DefaultDouble = defaultDouble,
                DefaultFloat = defaultFloat,
                MinInt = minInt,
                MaxInt = maxInt,
                MinDouble = minDouble,
                MaxDouble = maxDouble,
                MinFloat = minFloat,
                MaxFloat = maxFloat,
                Step = step,
                WithStepper = withStepper,
                VisibilityKey = visibilityKey
            })
        { }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;

            var stack = FluentConfigComponentFactory.CreateTitledStack(Title, Description);
            ResolveInitialValue(context, out var tag, out var initialText);

            var tb = new System.Windows.Controls.TextBox
            {
                Tag = tag,
                Text = initialText,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            if (string.Equals(WidthMode, "full", StringComparison.OrdinalIgnoreCase))
            {
                if (InputType == InputValidation.InputType.String)
                    tb.MinWidth = 200;
            }
            else if (!string.IsNullOrEmpty(WidthMode) && double.TryParse(WidthMode, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var w) && w > 0)
            {
                tb.Width = w;
                tb.HorizontalAlignment = HorizontalAlignment.Left;
                if (InputType == InputValidation.InputType.String)
                    tb.MinWidth = Math.Min(200, w);
            }
            else
            {
                tb.Width = 80;
                tb.HorizontalAlignment = HorizontalAlignment.Left;
                if (InputType == InputValidation.InputType.String)
                    tb.MinWidth = 200;
            }

            if (InputType != InputValidation.InputType.String)
            {
                InputValidation.CreatePreviewTextInputHandler(tb, InputType);
                InputValidation.AddDataObjectPastingHandler(tb, InputType);
            }

            AttachTextChangedHandler(tb, context);
            context.Registry.Register(SaveKey, tb);

            if (ShouldShowStepper())
            {
                stack.Children.Add(CreateStepperRow(tb, context));
            }
            else
            {
                stack.Children.Add(tb);
            }

            panel.Children.Add(stack);
        }

        private bool ShouldShowStepper()
        {
            return InputType == InputValidation.InputType.Int
                || (WithStepper && (InputType == InputValidation.InputType.Double || InputType == InputValidation.InputType.Float));
        }

        private void ResolveInitialValue(IRenderContext context, out string tag, out string initialText)
        {
            tag = SaveKey;
            initialText = DefaultString;
            var token = context.GetSetting(SaveKey);

            switch (InputType)
            {
                case InputValidation.InputType.Int:
                    tag = FluentConfigTags.IntegerPrefix + SaveKey;
                    int iv = DefaultInt;
                    if (token != null && IsNumericToken(token))
                    {
                        try { iv = token.ToObject<int>(); }
                        catch (Exception ex) { context.Log($"InputElement: failed to parse int for '{SaveKey}': {ex.Message}"); }
                    }
                    initialText = Math.Max(MinInt, Math.Min(MaxInt, iv)).ToString();
                    break;

                case InputValidation.InputType.Double:
                    tag = FluentConfigTags.DoublePrefix + SaveKey;
                    double dv = DefaultDouble;
                    if (token != null && IsNumericToken(token))
                    {
                        try { dv = token.ToObject<double>(); }
                        catch (Exception ex) { context.Log($"InputElement: failed to parse double for '{SaveKey}': {ex.Message}"); }
                    }
                    initialText = InputValidation.FormatDouble(Math.Max(MinDouble, Math.Min(MaxDouble, dv)));
                    break;

                case InputValidation.InputType.Float:
                    tag = FluentConfigTags.FloatPrefix + SaveKey;
                    float fv = DefaultFloat;
                    if (token != null && IsNumericToken(token))
                    {
                        try { fv = token.ToObject<float>(); }
                        catch (Exception ex) { context.Log($"InputElement: failed to parse float for '{SaveKey}': {ex.Message}"); }
                    }
                    initialText = InputValidation.FormatFloat(Math.Max(MinFloat, Math.Min(MaxFloat, fv)));
                    break;

                default:
                    if (token != null) initialText = token.ToString() ?? DefaultString;
                    break;
            }
        }

        private static bool IsNumericToken(Newtonsoft.Json.Linq.JToken token)
        {
            return token.Type == Newtonsoft.Json.Linq.JTokenType.Integer
                || token.Type == Newtonsoft.Json.Linq.JTokenType.Float;
        }

        private void AttachTextChangedHandler(System.Windows.Controls.TextBox tb, IRenderContext context)
        {
            bool isUpdating = false;
            tb.TextChanged += (s, e) =>
            {
                if (isUpdating) return;
                isUpdating = true;
                try
                {
                    context.MarkDirty();
                    if (InputType == InputValidation.InputType.Int)
                    {
                        if (InputValidation.TryParseInt(tb.Text, out var v, MinInt, MaxInt))
                            tb.Text = v.ToString();
                    }
                    else if (InputType == InputValidation.InputType.Double)
                    {
                        if (InputValidation.TryParseDouble(tb.Text, out var v, MinDouble, MaxDouble))
                            tb.Text = InputValidation.FormatDouble(v);
                    }
                    else if (InputType == InputValidation.InputType.Float)
                    {
                        if (InputValidation.TryParseFloat(tb.Text, out var v, MinFloat, MaxFloat))
                            tb.Text = InputValidation.FormatFloat(v);
                    }
                }
                finally { isUpdating = false; }
            };
        }

        private Grid CreateStepperRow(System.Windows.Controls.TextBox tb, IRenderContext context)
        {
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var minusBtn = CreateStepperButton("−");
            minusBtn.Click += (s, ev) => ApplyStep(tb, context, -1);

            var plusBtn = CreateStepperButton("+");
            plusBtn.Click += (s, ev) => ApplyStep(tb, context, +1);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
            buttonPanel.Children.Add(minusBtn);
            buttonPanel.Children.Add(plusBtn);

            Grid.SetColumn(tb, 0);
            Grid.SetColumn(buttonPanel, 1);
            row.Children.Add(tb);
            row.Children.Add(buttonPanel);

            return row;
        }

        private static Button CreateStepperButton(string symbol)
        {
            return new Button
            {
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = symbol,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                Width = 28,
                Height = 28,
                Margin = new Thickness(0, 4, symbol == "−" ? 2 : 0, 0),
                Padding = new Thickness(0)
            };
        }

        private void ApplyStep(System.Windows.Controls.TextBox tb, IRenderContext context, int direction)
        {
            switch (InputType)
            {
                case InputValidation.InputType.Int:
                    if (InputValidation.TryParseInt(tb.Text, out var iv, MinInt, MaxInt))
                    {
                        iv = direction > 0 ? Math.Min(MaxInt, iv + 1) : Math.Max(MinInt, iv - 1);
                        tb.Text = iv.ToString();
                        context.MarkDirty();
                    }
                    break;

                case InputValidation.InputType.Double:
                    if (InputValidation.TryParseDouble(tb.Text, out var dv, MinDouble, MaxDouble))
                    {
                        dv = direction > 0 ? Math.Min(MaxDouble, dv + Step) : Math.Max(MinDouble, dv - Step);
                        tb.Text = InputValidation.FormatDouble(dv);
                        context.MarkDirty();
                    }
                    break;

                case InputValidation.InputType.Float:
                    if (InputValidation.TryParseFloat(tb.Text, out var fv, MinFloat, MaxFloat))
                    {
                        fv = direction > 0 ? Math.Min(MaxFloat, fv + (float)Step) : Math.Max(MinFloat, fv - (float)Step);
                        tb.Text = InputValidation.FormatFloat(fv);
                        context.MarkDirty();
                    }
                    break;
            }
        }
    }
}
