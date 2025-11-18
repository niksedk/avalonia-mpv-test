using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;

namespace AvaloniaApplication1.Views.Controls;

// A simple NativeControlHost that creates a native child window for libmpv to render into.
public class MpvHost : NativeControlHost
{
    public static readonly StyledProperty<nint?> WindowHandleProperty =
        AvaloniaProperty.Register<MpvHost, nint?>(nameof(WindowHandle));

    public static readonly StyledProperty<string?> HandleDescriptorProperty =
        AvaloniaProperty.Register<MpvHost, string?>(nameof(HandleDescriptor));

    public nint? WindowHandle
    {
        get => GetValue(WindowHandleProperty);
        private set => SetValue(WindowHandleProperty, value);
    }

    public string? HandleDescriptor
    {
        get => GetValue(HandleDescriptorProperty);
        private set => SetValue(HandleDescriptorProperty, value);
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var created = base.CreateNativeControlCore(parent);
        if (OperatingSystem.IsWindows())
        {
            // parent.Handle may be a string (Avalonia <=11.0) or nint (newer). Read as string if possible.
            nint parentHwnd = 0;
            //if (parent.Handle is string s)
            //{
            //    if (!string.IsNullOrEmpty(s))
            //    {
            //        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            //            parentHwnd = (nint)Convert.ToInt64(s.Substring(2), 16);
            //        else
            //            parentHwnd = (nint)Convert.ToInt64(s, CultureInfo.InvariantCulture);
            //    }
            //}
            //else if (parent.Handle is nint hn)
            //{
            parentHwnd = parent.Handle;
            // }
            var child = Win32.CreateChildWindow(parentHwnd);
            WindowHandle = child;
            HandleDescriptor = "HWND";
            return new PlatformHandle(child, "HWND");
        }

        // On macOS and Linux, reuse Avalonia's native child surface/view created by the base implementation.
        WindowHandle = created.Handle;
        HandleDescriptor = created.HandleDescriptor;
        return created;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (OperatingSystem.IsWindows() && WindowHandle is nint hwnd)
        {
            Win32.DestroyChildWindow(hwnd);
            WindowHandle = null;
            HandleDescriptor = null;
        }
        else
        {
            // On non-Windows, base-created view/window will be cleaned up by base implementation.
            WindowHandle = null;
            HandleDescriptor = null;
        }

        base.DestroyNativeControlCore(control);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (OperatingSystem.IsWindows() && WindowHandle is nint hwnd)
        {
            Win32.ResizeChild(hwnd, (int)Math.Max(0, size.Width), (int)Math.Max(0, size.Height));
        }

        return size;
    }

    private static class Win32
    {
        private const string User32 = "user32.dll";

        [System.Runtime.InteropServices.DllImport(User32, SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern nint CreateWindowExW(
            int dwExStyle,
            string lpClassName,
            string? lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            nint hWndParent,
            nint hMenu,
            nint hInstance,
            nint lpParam);

        [System.Runtime.InteropServices.DllImport(User32, SetLastError = true)]
        private static extern bool DestroyWindow(nint hWnd);

        [System.Runtime.InteropServices.DllImport(User32, SetLastError = true)]
        private static extern bool MoveWindow(nint hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        // GetModuleHandleW is exported by kernel32.dll, not user32.dll
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern nint GetModuleHandleW(string? lpModuleName);

        private const int WS_CHILD = 0x40000000;
        private const int WS_VISIBLE = 0x10000000;

        public static nint CreateChildWindow(nint parent)
        {
            // Use built-in STATIC control as a dummy child window to host mpv output.
            return CreateWindowExW(
                0,
                "STATIC",
                null,
                WS_CHILD | WS_VISIBLE,
                0, 0, 100, 100,
                parent,
                0,
                GetModuleHandleW(null),
                0);
        }

        public static void ResizeChild(nint hwnd, int width, int height)
        {
            MoveWindow(hwnd, 0, 0, Math.Max(1, width), Math.Max(1, height), true);
        }

        public static void DestroyChildWindow(nint hwnd) => DestroyWindow(hwnd);
    }
}