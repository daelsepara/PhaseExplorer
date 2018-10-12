using System;
using System.IO;
using System.Runtime.InteropServices;

public static class LensPhaseDLL
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate double* FPhase();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FRelease();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void FCompute(int argc, void** argv);

    static int Width;
    static int Height;
    static double Z;
    static double Pitch;
    static double Wavelength;

    public static void SetParameters(int width, int height, double z, double pitch, double wavelength)
    {
        Width = width;
        Height = height;
        Z = z;
        Pitch = pitch;
        Wavelength = wavelength;
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

            void** Parameters = stackalloc void*[5];

            var width = Width;
            var height = Height;
            var z = Z;
            var wavelength = Wavelength;
            var pitch = Pitch;

            Parameters[0] = &width;
            Parameters[1] = &height;
            Parameters[2] = &z;
            Parameters[3] = &wavelength;
            Parameters[4] = &pitch;

            Compute(5, Parameters);

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
