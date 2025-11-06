using System;

namespace AvaloniaApplication1.Video;

public sealed class MpvPlayer : IDisposable
{
    private IntPtr _mpv;
    private bool _disposed;

    public MpvPlayer()
    {
        _mpv = LibMpv.mpv_create();
        if (_mpv == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create mpv context. Ensure libmpv-2.dll is available.");
        }
    }

    public void InitializeWithWindowHandle(nint hwnd)
    {
        EnsureNotDisposed();
        // Configure video output before initialize.
        var err = LibMpv.mpv_set_option_string(_mpv, "vo", "gpu");
        if (err < 0)
        {
            throw new InvalidOperationException(LibMpv.GetErrorString(err));
        }

        if (OperatingSystem.IsWindows())
        {
            // Prefer d3d11 on Windows; if unsupported, mpv will fallback.
            LibMpv.mpv_set_option_string(_mpv, "gpu-api", "d3d11");
        }
        else if (OperatingSystem.IsMacOS())
        {
            // On macOS the 'wid' accepts NSView* pointer value.
            LibMpv.mpv_set_option_string(_mpv, "gpu-api", "metal");
        }

        if (hwnd != 0)
        {
            // Tell mpv to render into our embedded native child surface
            err = LibMpv.mpv_set_option_string(_mpv, "wid", hwnd.ToInt64().ToString());
            if (err < 0)
            {
                throw new InvalidOperationException(LibMpv.GetErrorString(err));
            }
        }

        err = LibMpv.mpv_initialize(_mpv);
        if (err < 0)
        {
            throw new InvalidOperationException(LibMpv.GetErrorString(err));
        }
    }

    public void LoadFile(string path)
    {
        EnsureNotDisposed();
        var escaped = path.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var err = LibMpv.mpv_command_string(_mpv, $"loadfile \"{escaped}\"");
        if (err < 0)
        {
            throw new InvalidOperationException(LibMpv.GetErrorString(err));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_mpv != IntPtr.Zero)
        {
            LibMpv.mpv_terminate_destroy(_mpv);
            _mpv = IntPtr.Zero;
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MpvPlayer));
        }
    }
}
