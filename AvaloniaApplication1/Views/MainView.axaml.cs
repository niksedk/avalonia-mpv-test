using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication1.Video;
using AvaloniaApplication1.Views.Controls;

namespace AvaloniaApplication1.Views;

public partial class MainView : UserControl
{
    private MpvPlayer? _player;
    private bool _playerInitialized;

    public MainView()
    {
        InitializeComponent();
        OpenButton.Click += OnOpenClicked;
        this.AttachedToVisualTree += OnAttached;
        this.DetachedFromVisualTree += OnDetached;
    }

    private async void OnOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (_player == null || !_playerInitialized) return;
        var ofd = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter { Name = "Video", Extensions = { "mp4","mkv","avi","mov","webm","ts","m2ts" } },
                new FileDialogFilter { Name = "All", Extensions = { "*" } }
            }
        };
        var paths = await ofd.ShowAsync(VisualRoot as Window);
        if (paths != null && paths.Length > 0)
        {
            _player.LoadFile(paths[0]);
        }
    }

    private void OnAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_player != null) return;
        _player = new MpvPlayer();

        // Initialize when the native HWND becomes available
        MpvHost.PropertyChanged += MpvHostOnPropertyChanged;
        TryInitializePlayerWithCurrentHandle();
    }

    private void MpvHostOnPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MpvHost.WindowHandleProperty)
        {
            var hwndOpt = (nint?)e.NewValue;
            if (!_playerInitialized && _player != null && hwndOpt is nint hwnd)
            {
                _player.InitializeWithWindowHandle(hwnd);
                _playerInitialized = true;
            }
        }
    }

    private void TryInitializePlayerWithCurrentHandle()
    {
        if (_player != null && !_playerInitialized && MpvHost.WindowHandle is nint hwnd)
        {
            _player.InitializeWithWindowHandle(hwnd);
            _playerInitialized = true;
        }
    }

    private void OnDetached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        MpvHost.PropertyChanged -= MpvHostOnPropertyChanged;
        _player?.Dispose();
        _player = null;
        _playerInitialized = false;
    }
}
