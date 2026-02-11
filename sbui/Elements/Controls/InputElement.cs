using System;
using System.Windows;
using System.Windows.Controls;
using Sbui.Components;
using Sbui.Helpers;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
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

        public InputElement(
            string title, string description, string tabName, string saveKey,
            InputValidation.InputType inputType,
            string defaultString, int defaultInt, double defaultDouble, float defaultFloat,
            int minInt, int maxInt, double minDouble, double maxDouble, float minFloat, float maxFloat,
            double step, bool withStepper,
            string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            InputType = inputType;
            DefaultString = defaultString ?? "";
            DefaultInt = defaultInt;
            DefaultDouble = defaultDouble;
            DefaultFloat = defaultFloat;
            MinInt = minInt;
            MaxInt = maxInt;
            MinDouble = minDouble;
            MaxDouble = maxDouble;
            MinFloat = minFloat;
            MaxFloat = maxFloat;
            Step = step;
            WithStepper = withStepper && (inputType == InputValidation.InputType.Double || inputType == InputValidation.InputType.Float);
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;

            var stack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 8, 0, 0) };
            stack.Children.Add(SbuiComponentFactory.CreateTitleTextBlock(Title));
            if (!string.IsNullOrEmpty(Description))
                stack.Children.Add(SbuiComponentFactory.CreateDescriptionTextBlock(Description));

            string tag = SaveKey;
            string initialText = DefaultString;

            if (InputType == InputValidation.InputType.Int)
            {
                tag = SbuiTags.IntegerPrefix + SaveKey;
                var token = context.GetSetting(SaveKey);
                int val = DefaultInt;
                if (token != null && (token.Type == Newtonsoft.Json.Linq.JTokenType.Integer || token.Type == Newtonsoft.Json.Linq.JTokenType.Float))
                {
                    try { val = token.ToObject<int>(); }
                    catch (Exception ex) { context.Log($"InputElement: failed to parse int for '{SaveKey}': {ex.Message}"); }
                }
                val = Math.Max(MinInt, Math.Min(MaxInt, val));
                initialText = val.ToString();
            }
            else if (InputType == InputValidation.InputType.Double)
            {
                tag = SbuiTags.DoublePrefix + SaveKey;
                var token = context.GetSetting(SaveKey);
                double val = DefaultDouble;
                if (token != null && (token.Type == Newtonsoft.Json.Linq.JTokenType.Float || token.Type == Newtonsoft.Json.Linq.JTokenType.Integer))
                {
                    try { val = token.ToObject<double>(); }
                    catch (Exception ex) { context.Log($"InputElement: failed to parse double for '{SaveKey}': {ex.Message}"); }
                }
                val = Math.Max(MinDouble, Math.Min(MaxDouble, val));
                initialText = InputValidation.FormatDouble(val);
            }
            else if (InputType == InputValidation.InputType.Float)
            {
                tag = SbuiTags.FloatPrefix + SaveKey;
                var token = context.GetSetting(SaveKey);
                float val = DefaultFloat;
                if (token != null && (token.Type == Newtonsoft.Json.Linq.JTokenType.Float || token.Type == Newtonsoft.Json.Linq.JTokenType.Integer))
                {
                    try { val = token.ToObject<float>(); }
                    catch (Exception ex) { context.Log($"InputElement: failed to parse float for '{SaveKey}': {ex.Message}"); }
                }
                val = Math.Max(MinFloat, Math.Min(MaxFloat, val));
                initialText = InputValidation.FormatFloat(val);
            }
            else
            {
                var token = context.GetSetting(SaveKey);
                if (token != null) initialText = token.ToString() ?? DefaultString;
            }

            var tb = new System.Windows.Controls.TextBox
            {
                Tag = tag,
                Text = initialText,
                Width = 80,
                Margin = new Thickness(0, 4, 8, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            if (InputType == InputValidation.InputType.Int || InputType == InputValidation.InputType.Double || InputType == InputValidation.InputType.Float)
            {
                InputValidation.CreatePreviewTextInputHandler(tb, InputType);
                InputValidation.AddDataObjectPastingHandler(tb, InputType);
            }

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

            context.Registry.Register(SaveKey, tb);

            if (WithStepper && (InputType == InputValidation.InputType.Double || InputType == InputValidation.InputType.Float))
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var minusBtn = new Button
                {
                    Content = new System.Windows.Controls.TextBlock { Text = "−", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Width = 28, Height = 28, Margin = new Thickness(0, 4, 2, 0), Padding = new Thickness(0)
                };
                minusBtn.Click += (s, ev) =>
                {
                    if (InputType == InputValidation.InputType.Double && InputValidation.TryParseDouble(tb.Text, out var v, MinDouble, MaxDouble))
                    {
                        v = Math.Max(MinDouble, v - Step);
                        tb.Text = InputValidation.FormatDouble(v);
                        context.MarkDirty();
                    }
                    else if (InputType == InputValidation.InputType.Float && InputValidation.TryParseFloat(tb.Text, out var vf, MinFloat, MaxFloat))
                    {
                        vf = Math.Max(MinFloat, vf - (float)Step);
                        tb.Text = InputValidation.FormatFloat(vf);
                        context.MarkDirty();
                    }
                };
                var plusBtn = new Button
                {
                    Content = new System.Windows.Controls.TextBlock { Text = "+", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Width = 28, Height = 28, Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(0)
                };
                plusBtn.Click += (s, ev) =>
                {
                    if (InputType == InputValidation.InputType.Double && InputValidation.TryParseDouble(tb.Text, out var v, MinDouble, MaxDouble))
                    {
                        v = Math.Min(MaxDouble, v + Step);
                        tb.Text = InputValidation.FormatDouble(v);
                        context.MarkDirty();
                    }
                    else if (InputType == InputValidation.InputType.Float && InputValidation.TryParseFloat(tb.Text, out var vf, MinFloat, MaxFloat))
                    {
                        vf = Math.Min(MaxFloat, vf + (float)Step);
                        tb.Text = InputValidation.FormatFloat(vf);
                        context.MarkDirty();
                    }
                };
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
                buttonPanel.Children.Add(minusBtn);
                buttonPanel.Children.Add(plusBtn);
                Grid.SetColumn(tb, 0);
                Grid.SetColumn(buttonPanel, 1);
                row.Children.Add(tb);
                row.Children.Add(buttonPanel);
                stack.Children.Add(row);
            }
            else if (InputType == InputValidation.InputType.Int)
            {
                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                var minusBtn = new Button
                {
                    Content = new System.Windows.Controls.TextBlock { Text = "−", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Width = 28, Height = 28, Margin = new Thickness(0, 4, 2, 0), Padding = new Thickness(0)
                };
                minusBtn.Click += (s, ev) =>
                {
                    if (InputValidation.TryParseInt(tb.Text, out var v, MinInt, MaxInt))
                    {
                        v = Math.Max(MinInt, v - 1);
                        tb.Text = v.ToString();
                        context.MarkDirty();
                    }
                };
                var plusBtn = new Button
                {
                    Content = new System.Windows.Controls.TextBlock { Text = "+", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
                    Width = 28, Height = 28, Margin = new Thickness(0, 4, 0, 0), Padding = new Thickness(0)
                };
                plusBtn.Click += (s, ev) =>
                {
                    if (InputValidation.TryParseInt(tb.Text, out var v, MinInt, MaxInt))
                    {
                        v = Math.Min(MaxInt, v + 1);
                        tb.Text = v.ToString();
                        context.MarkDirty();
                    }
                };
                var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
                buttonPanel.Children.Add(minusBtn);
                buttonPanel.Children.Add(plusBtn);
                Grid.SetColumn(tb, 0);
                Grid.SetColumn(buttonPanel, 1);
                row.Children.Add(tb);
                row.Children.Add(buttonPanel);
                stack.Children.Add(row);
            }
            else
            {
                stack.Children.Add(tb);
            }

            if (InputType == InputValidation.InputType.String)
                tb.MinWidth = 200;

            panel.Children.Add(stack);
        }
    }
}
