using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using Readmd.Documents;
using Readmd.Rendering;

namespace Readmd;

public partial class MainWindow : Window
{
    private readonly MarkdownRenderer _renderer = new();
    private int _blankTabCount;

    public MainWindow()
    {
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenCommand_Executed));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Open, new KeyGesture(Key.O, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(ApplicationCommands.Close, new KeyGesture(Key.W, ModifierKeys.Control)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseCommand_Executed, CloseCommand_CanExecute));
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

        var closeButton = new Button
        {
            Content = "x",
            Width = 18,
            Height = 18,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            ToolTip = "Close tab",
            FontSize = 12,
            Focusable = false
        };
        closeButton.Click += CloseTabButton_Click;
        DockPanel.SetDock(closeButton, Dock.Right);

        var text = new TextBlock
        {
            Text = title,
            MaxWidth = 260,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
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
