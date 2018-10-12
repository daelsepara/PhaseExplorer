using Gdk;
using System;
using System.IO;
using System.Runtime.InteropServices;

public static class BlazedPhaseDLL
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate double* FPhase();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FRelease();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void FCompute(int argc, void** argv);

    static int Width;
    static int Height;
    static double X;
    static double Y;

    public static void SetParameters(int width, int height, double x, double y)
    {
        Width = width;
        Height = height;
        X = x;
        Y = y;
    }

    unsafe public static PhaseOutput ComputePhase(string dll)
    {
        var file = Common.OSTest.IsWindows() ? String.Format("./libphase++{0}.dll", dll) : (Common.OSTest.IsRunningOnMac() ? String.Format("./libphase++{0}.dylib", dll) : String.Format("./libphase++{0}.so", dll));

        if (File.Exists(file))
        {
            IntPtr pLibrary = DLLLoader.LoadLibrary(file);
            IntPtr pCompute = DLLLoader.GetProcAddress(pLibrary, "Compute");
            IntPtr pPhase = DLLLoader.GetProcAddress(pLibrary, "Phase");
            IntPtr pRelease = DLLLoader.GetProcAddress(pLibrary, "Release");

            var Compute = DLLLoader.LoadFunction<FCompute>(pCompute);
            var Phase = DLLLoader.LoadFunction<FPhase>(pPhase);
            var Release = DLLLoader.LoadFunction<FRelease>(pRelease);

            void** Parameters = stackalloc void*[4];

            var width = Width;
            var height = Height;
            var x = X;
            var y = Y;

            Parameters[0] = &width;
            Parameters[1] = &height;
            Parameters[2] = &x;
            Parameters[3] = &y;

            Compute(4, Parameters);

            var output = Phase();

            var phase = new PhaseOutput(output, width * height);

            // Free resources
            Release();

            DLLLoader.FreeLibrary(pLibrary);

            return phase;
        }

        var length = Width * Height > 0 ? Width * Height : 256 * 256;

        return new PhaseOutput(length);
    }
}
