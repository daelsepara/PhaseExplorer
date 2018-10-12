using Gdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public static class SpotsDLL
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate double* FPhase();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void FRelease();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void FCompute(int argc, void** argv);

    static List<Point> Spots = new List<Point>();
    static int NFFT = 1024;
    static bool Translate = true;

    static int Width;
    static int Height;

    public static void SetParameters(int width, int height, int nfft, List<Point> points, bool translate)
    {
        Width = width;
        Height = height;
        NFFT = nfft;

        Spots.Clear();
        Spots.AddRange(points);

        Translate = translate;
    }

    unsafe static void GetPoints(List<Point> Points, int width, int height, out double* pointsX, out double* pointsY)
    {
        pointsX = (double*)Marshal.AllocHGlobal(Points.Count * sizeof(double));
        pointsY = (double*)Marshal.AllocHGlobal(Points.Count * sizeof(double));

        int index = 0;

        int OriginX = Translate ? width / 2 : 0;
        int OriginY = Translate ? height / 2 : 0;

        foreach (var point in Points)
        {
            double x = Convert.ToDouble(point.X - OriginX);
            double y = Convert.ToDouble(point.Y - OriginY);

            pointsX[index] = x;
            pointsY[index] = y;

            index++;
        }
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

            double* SpotsX;
            double* SpotsY;

            GetPoints(Spots, Width, Height, out SpotsX, out SpotsY);

            void** Parameters = stackalloc void*[6];

            var width = Width;
            var height = Height;
            var nfft = Math.Max(NFFT, Math.Max(Width, Height));
            var N = Spots.Count;

            Parameters[0] = &width;
            Parameters[1] = &height;
            Parameters[2] = &nfft;
            Parameters[3] = &N;
            Parameters[4] = SpotsX;
            Parameters[5] = SpotsY;

            Compute(6, Parameters);

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
