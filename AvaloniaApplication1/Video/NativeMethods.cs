using System;
using System.Runtime.InteropServices;

namespace Nikse.SubtitleEdit.Logic
{
    internal static class NativeMethods
    {
        // Win32 API functions for dynamically loading DLLs
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        internal static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, BestFitMapping = false)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);
        

        // Linux
        internal const int LC_NUMERIC = 1;

        internal const int RTLD_NOW = 0x0001;
        internal const int RTLD_GLOBAL = 0x0100;

        [DllImport("libc.so.6")]
        internal static extern IntPtr setlocale(int category, string locale);

        [DllImport("libdl.so.2")]
        internal static extern IntPtr dlopen(string filename, int flags);

        [DllImport("libdl.so.2")]
        internal static extern IntPtr dlclose(IntPtr handle);

        [DllImport("libdl.so.2")]
        internal static extern IntPtr dlsym(IntPtr handle, string symbol);


        // cross-platform wrappers
        internal static IntPtr CrossLoadLibrary(string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return LoadLibrary(fileName);
            }

            return dlopen(fileName, RTLD_NOW | RTLD_GLOBAL);
        }

        internal static void CrossFreeLibrary(IntPtr handle)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FreeLibrary(handle);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                dlclose(handle);
            }
        }

        internal static IntPtr CrossGetProcAddress(IntPtr handle, string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetProcAddress(handle, name);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return dlsym(handle, name);
            }

            throw new PlatformNotSupportedException("Unsupported OS platform.");
        }

        internal static object? GetDllType(nint handle, Type type, string name)
        {
            var address = NativeMethods.CrossGetProcAddress(handle, name);
            return address != IntPtr.Zero ? Marshal.GetDelegateForFunctionPointer(address, type) : null;
        }
    }
}
