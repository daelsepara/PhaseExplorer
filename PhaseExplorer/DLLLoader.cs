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
        return Common.OSTest.IsWindows()
            ? Windows.DLOpen(libname)
            : Common.OSTest.IsRunningOnMac() ? OSX.DLOpen(libname, RTLD_GLOBAL | RTLD_LAZY) : Linux.DLOpen(libname, RTLD_GLOBAL | RTLD_LAZY);
    }

    public static IntPtr GetProcAddress(IntPtr library, string function)
    {
        var ret = IntPtr.Zero;

        ret = Common.OSTest.IsWindows()
            ? Windows.DLSym(library, function)
            : Common.OSTest.IsRunningOnMac() ? OSX.DLSym(library, function) : Linux.DLSym(library, function);

        return ret;
    }

    public static T LoadFunction<T>(IntPtr procaddress)
    {
        return procaddress == IntPtr.Zero ? default(T) : Marshal.GetDelegateForFunctionPointer<T>(procaddress);
    }

    public static int FreeLibrary(IntPtr handle)
    {
        return Common.OSTest.IsWindows()
            ? Windows.DLClose(handle) ? 0 : 1
            : Common.OSTest.IsRunningOnMac() ? OSX.DLClose(handle) : Linux.DLClose(handle);
    }
}
