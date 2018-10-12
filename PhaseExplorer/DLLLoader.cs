using System;
using System.Runtime.InteropServices;

// adapted from: https://github.com/GtkSharp/GtkSharp/blob/develop/Source/Libs/Shared/FuncLoader.cs
class DLLLoader
{
    static class Windows
    {
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true, EntryPoint = "GetProcAddress")]
        public static extern IntPtr DLSym(IntPtr handle, string symbol);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibrary")]
        public static extern IntPtr DLOpen(string path);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "FreeLibrary")]
        public static extern bool DLClose(IntPtr handle);
    }

    static class Linux
    {
        [DllImport("libdl", EntryPoint = "dlopen")]
        public static extern IntPtr DLOpen(string path, int flags);

        [DllImport("libdl", EntryPoint = "dlsym")]
        public static extern IntPtr DLSym(IntPtr handle, string symbol);

        [DllImport("libdl", EntryPoint = "dlclose")]
        public static extern int DLClose(IntPtr handle);
    }

    static class OSX
    {
        [DllImport("libSystem", EntryPoint = "dlopen")]
        public static extern IntPtr DLOpen(string path, int flags);

        [DllImport("libSystem", EntryPoint = "dlsym")]
        public static extern IntPtr DLSym(IntPtr handle, string symbol);

        [DllImport("libSystem", EntryPoint = "dlclose")]
        public static extern int DLClose(IntPtr handle);
    }

    const int RTLD_LAZY = 0x0001;
    const int RTLD_GLOBAL = 0x0100;

    public static IntPtr LoadLibrary(string libname)
    {
        if (Common.OSTest.IsWindows())
        {
            return Windows.DLOpen(libname);
        }

        if (Common.OSTest.IsRunningOnMac())
        {
            return OSX.DLOpen(libname, RTLD_GLOBAL | RTLD_LAZY);
        }

        return Linux.DLOpen(libname, RTLD_GLOBAL | RTLD_LAZY);
    }

    public static IntPtr GetProcAddress(IntPtr library, string function)
    {
        var ret = IntPtr.Zero;

        if (Common.OSTest.IsWindows())
        {
            ret = Windows.DLSym(library, function);
        }
        else
        {
            if (Common.OSTest.IsRunningOnMac())
            {
                ret = OSX.DLSym(library, function);
            }
            else
            {
                ret = Linux.DLSym(library, function);
            }
        }

        return ret;
    }

    public static T LoadFunction<T>(IntPtr procaddress)
    {
        if (procaddress == IntPtr.Zero)
            return default(T);

        return Marshal.GetDelegateForFunctionPointer<T>(procaddress);
    }

    public static int FreeLibrary(IntPtr handle)
    {
        if (Common.OSTest.IsWindows())
        {
            var code = Windows.DLClose(handle);

            return code ? 0 : 1;
        }

        if (Common.OSTest.IsRunningOnMac())
        {
            return OSX.DLClose(handle);
        }

        return Linux.DLClose(handle);
    }
}
