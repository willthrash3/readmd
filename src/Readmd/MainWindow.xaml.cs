using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using Readmd.Documents;
using Readmd.Infrastructure;
using Readmd.Rendering;

namespace Readmd;

public partial class MainWindow : Window
{
    private readonly MarkdownRenderer _renderer = new();
    private int _blankTabCount;

    public MainWindow()
    {
        InitializeComponent();
        ApplySystemTheme();
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        Closed += MainWindow_Closed;
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenCommand_Executed));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Close, new KeyGesture(Key.W, ModifierKeys.Control)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseCommand_Executed, CloseCommand_CanExecute));
    }

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.Invoke(ApplySystemTheme);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
    }

    private void ApplySystemTheme()
    {
        var colors = SystemTheme.IsDarkMode()
            ? new Dictionary<string, Color>
            {
                ["AppSurface"] = Color.FromRgb(18, 22, 27),
                ["ChromeBackground"] = Color.FromRgb(27, 32, 38),
                ["ChromeBorder"] = Color.FromRgb(55, 64, 74),
                ["DocumentBackground"] = Color.FromRgb(20, 25, 30),
                ["AppText"] = Color.FromRgb(226, 232, 240),
                ["MutedText"] = Color.FromRgb(158, 170, 184),
                ["AccentBrush"] = Color.FromRgb(91, 184, 155),
                ["AccentSoftBrush"] = Color.FromRgb(31, 69, 59),
                ["HoverBackground"] = Color.FromRgb(43, 51, 60),
                ["PressedBackground"] = Color.FromRgb(53, 63, 73),
                ["AccentBorder"] = Color.FromRgb(55, 112, 95),
                ["CloseHoverBackground"] = Color.FromRgb(52, 61, 71),
                ["ClosePressedBackground"] = Color.FromRgb(64, 75, 86)
            }
            : new Dictionary<string, Color>
            {
                ["AppSurface"] = Color.FromRgb(245, 247, 250),
                ["ChromeBackground"] = Colors.White,
                ["ChromeBorder"] = Color.FromRgb(221, 227, 234),
                ["DocumentBackground"] = Color.FromRgb(250, 251, 252),
                ["AppText"] = Color.FromRgb(31, 41, 51),
                ["MutedText"] = Color.FromRgb(100, 113, 127),
                ["AccentBrush"] = Color.FromRgb(47, 125, 103),
                ["AccentSoftBrush"] = Color.FromRgb(225, 241, 236),
                ["HoverBackground"] = Color.FromRgb(236, 241, 245),
                ["PressedBackground"] = Color.FromRgb(221, 230, 236),
                ["AccentBorder"] = Color.FromRgb(183, 217, 207),
                ["CloseHoverBackground"] = Color.FromRgb(229, 235, 240),
                ["ClosePressedBackground"] = Color.FromRgb(214, 224, 231)
            };

        foreach (var (key, color) in colors)
        {
            Resources[key] = new SolidColorBrush(color);
        }

        Background = (Brush)Resources["AppSurface"];
        Foreground = (Brush)Resources["AppText"];
    }

    public void OpenBlankTab()
    {
        _blankTabCount++;
        var title = _blankTabCount == 1 ? "Untitled" : $"Untitled {_blankTabCount}";
        var document = RenderedDocument.Blank(title, _renderer.RenderDocument(string.Empty));
        AddDocumentTab(document);
    }

    public void OpenFiles(IEnumerable<string> paths)
    {
        var openedAny = false;
        foreach (var path in paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            AddDocumentTab(RenderedDocument.Load(path, _renderer));
            openedAny = true;
        }

        if (openedAny)
        {
            CloseInitialBlankTab();
        }
    }

    public void RestoreAndActivate()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void AddDocumentTab(RenderedDocument document)
    {
        var webView = new WebView2
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        var tab = new TabItem
        {
            Header = CreateTabHeader(document.Title),
            Content = webView,
            Tag = document
        };

        DocumentTabs.Items.Add(tab);
        DocumentTabs.SelectedItem = tab;
        Title = $"{document.Title} - Readmd";
        StatusText.Text = document.StatusText;

        _ = LoadWebViewAsync(webView, document.Html);
    }

    private static DockPanel CreateTabHeader(string title)
    {
        var header = new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0)
        };

        var closeIcon = new Path
        {
            Data = Geometry.Parse("M5 5 L13 13 M13 5 L5 13"),
            StrokeThickness = 1.8,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Width = 18,
            Height = 18,
            Stretch = Stretch.None
        };
        closeIcon.SetResourceReference(Shape.StrokeProperty, "MutedText");

        var closeButton = new Button
        {
            Content = closeIcon,
            ToolTip = "Close tab",
            Focusable = false
        };
        closeButton.SetResourceReference(FrameworkElement.StyleProperty, "TabCloseButtonStyle");
        closeButton.Click += CloseTabButton_Click;
        DockPanel.SetDock(closeButton, Dock.Right);

        var text = new TextBlock
        {
            Text = title,
            MaxWidth = 260,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontSize = 13
        };

        header.Children.Add(closeButton);
        header.Children.Add(text);
        return header;
    }

    private static async Task LoadWebViewAsync(WebView2 webView, string html)
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView.NavigateToString(html);
        }
        catch (Exception ex)
        {
            var fallback = MarkdownRenderer.RenderErrorDocument("WebView2 failed to load.", ex.Message);
            webView.NavigateToString(fallback);
        }
    }

    private void CloseInitialBlankTab()
    {
        if (DocumentTabs.Items.Count <= 1)
        {
            return;
        }

        foreach (TabItem tab in DocumentTabs.Items)
        {
            if (tab.Tag is RenderedDocument { IsBlank: true })
            {
                DocumentTabs.Items.Remove(tab);
                return;
            }
        }
    }

    private void CloseSelectedTab()
    {
        if (DocumentTabs.SelectedItem is not TabItem tab)
        {
            return;
        }

        DocumentTabs.Items.Remove(tab);
        if (DocumentTabs.Items.Count == 0)
        {
            OpenBlankTab();
        }
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => ShowOpenDialog();

    private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e) => ShowOpenDialog();

    private void CloseTabMenuItem_Click(object sender, RoutedEventArgs e) => CloseSelectedTab();

    private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e) => CloseSelectedTab();

    private void CloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = DocumentTabs.Items.Count > 0;

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowOpenDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Markdown files (*.md;*.markdown;*.mdown)|*.md;*.markdown;*.mdown|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Multiselect = true,
            Title = "Open Markdown File"
        };

        if (dialog.ShowDialog(this) == true)
        {
            OpenFiles(dialog.FileNames);
        }
    }

    private static void CloseTabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Parent: DockPanel header })
        {
            return;
        }

        var tabControl = FindAncestor<TabControl>(header);
        var tab = FindAncestor<TabItem>(header);
        if (tabControl is null || tab is null)
        {
            return;
        }

        tabControl.Items.Remove(tab);
        if (tabControl.Items.Count == 0 && Window.GetWindow(tabControl) is MainWindow window)
        {
            window.OpenBlankTab();
        }
    }

    private void DocumentTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DocumentTabs.SelectedItem is TabItem { Tag: RenderedDocument document })
        {
            Title = $"{document.Title} - Readmd";
            StatusText.Text = document.StatusText;
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current)
        where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
