using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using Reverse1999UrlCatcher.App.ViewModels;

namespace Reverse1999UrlCatcher.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyTheme(_viewModel.IsDarkTheme);
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.ClearSensitiveData();
        base.OnClosed(e);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDarkTheme))
        {
            ApplyTheme(_viewModel.IsDarkTheme);
        }
    }

    private void ApplyTheme(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            SetBrushColor("WindowBrush", Color.FromRgb(0x10, 0x14, 0x1B));
            SetBrushColor("TopBarBrush", Color.FromRgb(0x18, 0x1E, 0x28));
            SetBrushColor("PanelBrush", Color.FromRgb(0x1B, 0x23, 0x2F));
            SetBrushColor("PanelBorderBrush", Color.FromRgb(0x2E, 0x3A, 0x4C));
            SetBrushColor("InputBrush", Color.FromRgb(0x14, 0x1B, 0x26));
            SetBrushColor("InputBorderBrush", Color.FromRgb(0x3B, 0x4A, 0x5F));
            SetBrushColor("PrimaryTextBrush", Color.FromRgb(0xEC, 0xF1, 0xF8));
            SetBrushColor("SecondaryTextBrush", Color.FromRgb(0xA4, 0xB3, 0xC7));
            SetBrushColor("DisabledTextBrush", Color.FromRgb(0x78, 0x86, 0x9A));
            SetBrushColor("ButtonBrush", Color.FromRgb(0x1B, 0x23, 0x2F));
            SetBrushColor("ButtonHoverBrush", Color.FromRgb(0x23, 0x2E, 0x3F));
            SetBrushColor("ButtonDisabledBrush", Color.FromRgb(0x17, 0x1D, 0x27));
            SetBrushColor("ButtonBorderBrush", Color.FromRgb(0x3B, 0x4A, 0x5F));
            SetBrushColor("AccentBrush", Color.FromRgb(0x2D, 0x7D, 0xF1));
            SetBrushColor("AccentHoverBrush", Color.FromRgb(0x1F, 0x69, 0xD5));
            SetBrushColor("SuccessBrush", Color.FromRgb(0x2B, 0xC1, 0x5A));
            SetBrushColor("StatusBadgeBrush", Color.FromRgb(0x1E, 0x27, 0x34));
            SetBrushColor("LogBackgroundBrush", Color.FromRgb(0x14, 0x1A, 0x24));
            SetBrushColor("LogBorderBrush", Color.FromRgb(0x2F, 0x3A, 0x4D));
            return;
        }

        SetBrushColor("WindowBrush", Color.FromRgb(0xF3, 0xF6, 0xFC));
        SetBrushColor("TopBarBrush", Color.FromRgb(0xF8, 0xFA, 0xFF));
        SetBrushColor("PanelBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
        SetBrushColor("PanelBorderBrush", Color.FromRgb(0xD8, 0xDF, 0xEB));
        SetBrushColor("InputBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
        SetBrushColor("InputBorderBrush", Color.FromRgb(0xC8, 0xD3, 0xE2));
        SetBrushColor("PrimaryTextBrush", Color.FromRgb(0x16, 0x1B, 0x22));
        SetBrushColor("SecondaryTextBrush", Color.FromRgb(0x4A, 0x5C, 0x72));
        SetBrushColor("ButtonBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
        SetBrushColor("ButtonHoverBrush", Color.FromRgb(0xF2, 0xF6, 0xFF));
        SetBrushColor("ButtonDisabledBrush", Color.FromRgb(0xEE, 0xF2, 0xF7));
            SetBrushColor("ButtonBorderBrush", Color.FromRgb(0xC8, 0xD3, 0xE2));
        SetBrushColor("DisabledTextBrush", Color.FromRgb(0x8A, 0x98, 0xAD));
        SetBrushColor("AccentBrush", Color.FromRgb(0x15, 0x65, 0xD8));
        SetBrushColor("AccentHoverBrush", Color.FromRgb(0x0F, 0x55, 0xBA));
        SetBrushColor("SuccessBrush", Color.FromRgb(0x1F, 0xA4, 0x4A));
        SetBrushColor("StatusBadgeBrush", Color.FromRgb(0xF5, 0xF7, 0xFB));
        SetBrushColor("LogBackgroundBrush", Color.FromRgb(0xF9, 0xFB, 0xFF));
        SetBrushColor("LogBorderBrush", Color.FromRgb(0xD7, 0xE0, 0xED));
    }

    private void SetBrushColor(string key, Color color)
    {
        if (Resources.Contains(key))
        {
            Resources[key] = new SolidColorBrush(color);
        }
    }

    private void GitHubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true,
        });
        e.Handled = true;
    }
}
