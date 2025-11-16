using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using System;
using System.Runtime.InteropServices;

namespace AvaloniaApplication1.Video;

public class MpvOpenGLControl : OpenGlControlBase
{
    private MpvPlayer? _mpvPlayer;
    private bool _isInitialized;

    public MpvPlayer? Player => _mpvPlayer;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        base.OnOpenGlInit(gl);

        if (_mpvPlayer == null)
        {
            _mpvPlayer = new MpvPlayer();
            
            // Set up the GetProcAddress delegate for OpenGL
            _mpvPlayer.InitializeWithOpenGL((ctx, name) =>
            {
                try
                {
                    return gl.GetProcAddress(name);
                }
                catch
                {
                    return IntPtr.Zero;
                }
            });

            // Subscribe to render requests
            _mpvPlayer.RequestRender += OnMpvRequestRender;
            
            _isInitialized = true;
        }
    }

    private void OnMpvRequestRender()
    {
        // Request a redraw on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            RequestNextFrameRendering();
        }, DispatcherPriority.Render);
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (!_isInitialized || _mpvPlayer == null)
        {
            return;
        }

        var scaling = VisualRoot?.RenderScaling ?? 1.0;
        var size = Bounds.Size * scaling;
        var width = (int)size.Width;
        var height = (int)size.Height;

        if (width <= 0 || height <= 0)
        {
            return;
        }

        try
        {
            // Render mpv content to the framebuffer
            _mpvPlayer.RenderToFramebuffer(fb, width, height, flipY: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Render error: {ex.Message}");
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        if (_mpvPlayer != null)
        {
            _mpvPlayer.RequestRender -= OnMpvRequestRender;
            _mpvPlayer.Dispose();
            _mpvPlayer = null;
        }

        base.OnOpenGlDeinit(gl);
    }

    public void LoadFile(string path)
    {
        _mpvPlayer?.LoadFile(path);
    }
}