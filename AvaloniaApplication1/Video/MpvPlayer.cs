using Nikse.SubtitleEdit.Logic;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AvaloniaApplication1.Video;

public sealed class MpvPlayer : IDisposable
{
    public static string MpvPath = string.Empty;

    private IntPtr _library = IntPtr.Zero;
    private IntPtr _mpv = IntPtr.Zero;
    private bool _disposed;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MpvCreate();
    private MpvCreate? _mpvCreate;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MpvInitialize(IntPtr mpvHandle);
    private MpvInitialize _mpvInitialize;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MpvCommand(IntPtr mpvHandle, IntPtr utf8Strings);
    private MpvCommand _mpvCommand;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MpvWaitEvent(IntPtr mpvHandle, double wait);
    private MpvWaitEvent _mpvWaitEvent;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MpvSetOption(IntPtr mpvHandle, byte[] name, int format, ref ulong data);
    private MpvSetOption _mpvSetOption;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MpvSetOptionString(IntPtr mpvHandle, byte[] name, byte[] value);
    private MpvSetOptionString _mpvSetOptionString;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MpvGetPropertyString(IntPtr mpvHandle, byte[] name, int format, ref IntPtr data);
    private MpvGetPropertyString _mpvGetPropertyString;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MpvGetPropertyDouble(IntPtr mpvHandle, byte[] name, int format, ref double data);

    private MpvGetPropertyDouble _mpvGetPropertyDouble;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MpvSetProperty(IntPtr mpvHandle, byte[] name, int format, ref byte[] data);
    private MpvSetProperty _mpvSetProperty;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void MpvFree(IntPtr data);
    private MpvFree _mpvFree;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong MpvClientApiVersion();
    private MpvClientApiVersion _mpvClientApiVersion;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MpvErrorString(int error);
    private MpvErrorString _mpvErrorString;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr MpvTerminateDestroy(IntPtr mpvHandle);
    private MpvTerminateDestroy _mpvTerminateDestroy;

    public MpvPlayer()
    {
    }

    private static string[] GetLibraryNames()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return
            [
                "libmpv-2.dll",
            ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ["libmpv.so", "libmpv.2.so"];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return ["libmpv.dylib", "libmpv.2.dylib"];
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS platform.");
        }
    }

    private static string[] GetLibraryPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return
            [
                MpvPath,
                "C:\\git\\subtitleedit\\src\\ui\\bin\\Debug\\net48",
                string.Empty,
            ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return
            [
                MpvPath,
                "/lib64",
                "/usr/lib64",
                "/lib",
                "/usr/lib",
                "/lib/x86_64-linux-gnu",
                "/usr/lib/x86_64-linux-gnu",
                "/usr/local/lib",
            ];
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return
            [
                MpvPath,
                "/Applications/Subtitle Edit.app/Contents/Frameworks",
                "/opt/local/lib",
                "/usr/local/lib",
                "/opt/homebrew/lib",
                "/opt/lib",
            ];
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported OS platform.");
        }
    }

    private void LoadLibMpvMethods()
    {
        _mpvCreate = (MpvCreate)GetDllType(typeof(MpvCreate), "mpv_create");
        _mpvInitialize = (MpvInitialize)GetDllType(typeof(MpvInitialize), "mpv_initialize");
        _mpvWaitEvent = (MpvWaitEvent)GetDllType(typeof(MpvWaitEvent), "mpv_wait_event");
        _mpvCommand = (MpvCommand)GetDllType(typeof(MpvCommand), "mpv_command");
        _mpvSetOption = (MpvSetOption)GetDllType(typeof(MpvSetOption), "mpv_set_option");
        _mpvSetOptionString = (MpvSetOptionString)GetDllType(typeof(MpvSetOptionString), "mpv_set_option_string");
        _mpvGetPropertyString = (MpvGetPropertyString)GetDllType(typeof(MpvGetPropertyString), "mpv_get_property");
        _mpvGetPropertyDouble = (MpvGetPropertyDouble)GetDllType(typeof(MpvGetPropertyDouble), "mpv_get_property");
        _mpvSetProperty = (MpvSetProperty)GetDllType(typeof(MpvSetProperty), "mpv_set_property");
        _mpvFree = (MpvFree)GetDllType(typeof(MpvFree), "mpv_free");
        _mpvClientApiVersion = (MpvClientApiVersion)GetDllType(typeof(MpvClientApiVersion), "mpv_client_api_version");
        _mpvErrorString = (MpvErrorString)GetDllType(typeof(MpvErrorString), "mpv_error_string");
        _mpvTerminateDestroy = (MpvTerminateDestroy)GetDllType(typeof(MpvTerminateDestroy), "mpv_terminate_destroy");
    }

    private object GetDllType(Type type, string name)
    {
        var address = NativeMethods.CrossGetProcAddress(_library, name);
        return address != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer(address, type) : IntPtr.Zero;
    }

    private bool LoadLib()
    {
        foreach (var libName in GetLibraryNames())
        {
            foreach (var libPath in GetLibraryPaths())
            {
                var fullPath = Path.Combine(libPath, libName);
                if (File.Exists(fullPath))
                {
                    var libHandle = NativeMethods.CrossLoadLibrary(fullPath);
                    if (libHandle != IntPtr.Zero)
                    {
                        _library = libHandle;
                        LoadLibMpvMethods();
                        _mpv = _mpvCreate!.Invoke();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static byte[] GetUtf8Bytes(string s)
    {
        return Encoding.UTF8.GetBytes(s + "\0");
    }

    public string GetErrorString(int error)
    {
        var ptr = _mpvErrorString(error);
        return ptr == IntPtr.Zero ? $"mpv error {error}" : Marshal.PtrToStringUTF8(ptr) ?? $"mpv error {error}";
    }

    public int SetOptionString(string name, string value)
    {
        var nameBytes = GetUtf8Bytes(name);
        var valueBytes = GetUtf8Bytes(value);
        return _mpvSetOptionString(_mpv, nameBytes, valueBytes);
    }

    public static IntPtr AllocateUtf8IntPtrArrayWithSentinel(string[] arr, out IntPtr[] byteArrayPointers)
    {
        var numberOfStrings = arr.Length + 1; // add extra element for extra null pointer last (sentinel)
        byteArrayPointers = new IntPtr[numberOfStrings];
        IntPtr rootPointer = Marshal.AllocCoTaskMem(IntPtr.Size * numberOfStrings);
        for (var index = 0; index < arr.Length; index++)
        {
            var bytes = GetUtf8Bytes(arr[index]);
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
            byteArrayPointers[index] = unmanagedPointer;
        }
        Marshal.Copy(byteArrayPointers, 0, rootPointer, numberOfStrings);
        return rootPointer;
    }

    private int DoMpvCommand(params string[] args)
    {
        if (_mpv == IntPtr.Zero)
        {
            return 0;
        }

        var mainPtr = AllocateUtf8IntPtrArrayWithSentinel(args, out var byteArrayPointers);
        var result = _mpvCommand(_mpv, mainPtr);
        foreach (var ptr in byteArrayPointers)
        {
            Marshal.FreeHGlobal(ptr);
        }
        Marshal.FreeHGlobal(mainPtr);
        return result;
    }

    public void InitializeWithWindowHandle(nint hwnd)
    {
        LoadLib();
        EnsureNotDisposed();

        // Configure video output before initialize.
        var err = SetOptionString("vo", "gpu");
        if (err < 0)
        {
            throw new InvalidOperationException(GetErrorString(err));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Prefer d3d11 on Windows; if unsupported, mpv will fallback.
            SetOptionString("gpu-api", "d3d11");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS the 'wid' accepts NSView* pointer value.
            SetOptionString("gpu-api", "metal");
        }

        if (hwnd != 0)
        {
            // Tell mpv to render into our embedded native child surface
            err = SetOptionString("wid", hwnd.ToInt64().ToString());
            if (err < 0)
            {
                throw new InvalidOperationException(GetErrorString(err));
            }
        }

        err = _mpvInitialize(_mpv);
        if (err < 0)
        {
            throw new InvalidOperationException(GetErrorString(err));
        }
    }

    public void LoadFile(string path)
    {
        EnsureNotDisposed();
        var err = DoMpvCommand("loadfile", path);
        if (err < 0)
        {
            throw new InvalidOperationException(GetErrorString(err));
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
            _mpvTerminateDestroy.Invoke(_mpv);
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
