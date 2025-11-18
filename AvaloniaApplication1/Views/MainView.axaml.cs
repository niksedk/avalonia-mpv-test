using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication1.Video;
using System;

namespace AvaloniaApplication1.Views;

public partial class MainView : UserControl
{
    private MpvOpenGLControl? _openGLControl;
    private bool _playerInitialized;

    public MainView()
    {
        InitializeComponent();

        OpenButton.Click += OnOpenClicked;
        TogglePlayPauseButton.Click += OnTogglePlayPauseClicked;
        CloseVideoButton.Click += OnCloseClicked;
        this.AttachedToVisualTree += OnAttached;
        this.DetachedFromVisualTree += OnDetached;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        _openGLControl?.Unload();
    }

    private void OnTogglePlayPauseClicked(object? sender, RoutedEventArgs e)
    {
        _openGLControl?.TogglePlayPause();
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
            _openGLControl?.LoadFile(paths[0]);
        }
    }

    private void OnAttached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        if (_playerInitialized) return;

        // Use OpenGL control for all platforms (Windows, macOS, Linux)
        InitializeOpenGL();
    }

    private void InitializeOpenGL()
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

    private void OnDetached(object? sender, Avalonia.VisualTreeAttachmentEventArgs e)
    {
        _openGLControl = null; // Will be disposed by visual tree
        _playerInitialized = false;
    }
}
