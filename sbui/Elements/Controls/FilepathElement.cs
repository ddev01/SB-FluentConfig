using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Sbui.Components;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;

namespace Sbui.Elements
{
    public class FilepathElement : UIElement
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string SaveKey { get; set; }
        public string DefaultPath { get; set; }

        public FilepathElement(string title, string description, string tabName, string saveKey, string defaultPath, string visibilityKey = null)
        {
            Title = title;
            Description = description ?? "";
            TabName = tabName ?? "";
            SaveKey = saveKey;
            DefaultPath = defaultPath ?? "";
            VisibilityKey = visibilityKey;
        }

        public override void Render(IRenderContext context)
        {
            var panel = context.GetPanel(TabName);
            if (panel == null) return;
            var initial = context.GetSetting(SaveKey)?.ToString() ?? DefaultPath ?? "";
            var stack = SbuiComponentFactory.CreateTitledStack(Title, Description);
            var pathGrid = new Grid();
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tb = SbuiComponentFactory.CreateFilepathTextBox(SaveKey, initial);
            tb.TextChanged += (s, e) => context.MarkDirty();
            context.Registry.Register(SaveKey, tb);
            var browseBtn = new Button { Content = "Browse", Margin = new System.Windows.Thickness(0, 4, 0, 0), Padding = new System.Windows.Thickness(12, 6, 12, 6) };
            browseBtn.Click += (s, e) =>
            {
                var dlg = new OpenFileDialog { FileName = tb.Text };
                if (dlg.ShowDialog() == true)
                {
                    tb.Text = dlg.FileName;
                    context.MarkDirty();
                }
            };
            Grid.SetColumn(tb, 0);
            Grid.SetColumn(browseBtn, 1);
            pathGrid.Children.Add(tb);
            pathGrid.Children.Add(browseBtn);
            stack.Children.Add(pathGrid);
            panel.Children.Add(stack);
        }
    }
}
