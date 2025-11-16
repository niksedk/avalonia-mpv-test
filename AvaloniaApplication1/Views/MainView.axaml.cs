using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication1.Video;
using AvaloniaApplication1.Views.Controls;
using System.Runtime.InteropServices;

namespace AvaloniaApplication1.Views;

public partial class MainView : UserControl
{
    private MpvPlayer? _player;
    private MpvOpenGLControl? _openGLControl;
    private bool _playerInitialized;
    private readonly bool _useMacOSOpenGL;

    public MainView()
    {
        InitializeComponent();
        
        // Determine if we should use OpenGL approach (macOS)
        _useMacOSOpenGL = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        OpenButton.Click += OnOpenClicked;
        this.AttachedToVisualTree += OnAttached;
        this.DetachedFromVisualTree += OnDetached;
    }

    private async void OnOpenClicked(object? sender, RoutedEventArgs e)
    {
        if (!_playerInitialized) return;
        
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
            if (_useMacOSOpenGL)
            {
                _openGLControl?.LoadFile(paths[0]);
            }
            else
            {
                _player?.LoadFile(paths[0]);
            }
        }
    }

    private void OnAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_playerInitialized) return;

        if (_useMacOSOpenGL)
        {
            // macOS: Use OpenGL control
            InitializeMacOSOpenGL();
        }
        else
        {
            // Windows/Linux: Use window handle embedding
            InitializeWindowEmbedding();
        }
    }

    private void InitializeMacOSOpenGL()
    {
        // Create OpenGL control and replace MpvHost
        _openGLControl = new MpvOpenGLControl
        {
            [!WidthProperty] = MpvHost[!WidthProperty],
            [!HeightProperty] = MpvHost[!HeightProperty]
        };

        // Replace the MpvHost with the OpenGL control in the visual tree
        if (MpvHost.Parent is Panel panel)
        {
            var index = panel.Children.IndexOf(MpvHost);
            if (index >= 0)
            {
                panel.Children.RemoveAt(index);
                panel.Children.Insert(index, _openGLControl);
            }
        }

        _playerInitialized = true;
    }

    private void InitializeWindowEmbedding()
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
        if (_useMacOSOpenGL)
        {
            _openGLControl = null; // Will be disposed by visual tree
        }
        else
        {
            MpvHost.PropertyChanged -= MpvHostOnPropertyChanged;
            _player?.Dispose();
            _player = null;
        }
        
        _playerInitialized = false;
    }
}