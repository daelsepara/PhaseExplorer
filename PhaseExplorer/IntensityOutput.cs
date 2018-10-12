using System;
using System.Runtime.InteropServices;

public class IntensityOutput
{
    unsafe public double* Intensity;

    unsafe public IntensityOutput(int length)
    {
        Intensity = (double*)Marshal.AllocHGlobal(length * sizeof(double));

        for (var i = 0; i < length; i++)
            Intensity[i] = 0.0;
    }

    unsafe public IntensityOutput(double* intensity, int length)
    {
        Intensity = (double*)Marshal.AllocHGlobal(length * sizeof(double));

        for (var i = 0; i < length; i++)
            Intensity[i] = intensity[i];
    }

    unsafe public void Free()
    {
        if (Intensity != null)
        {
            Marshal.FreeHGlobal((IntPtr)Intensity);

            Intensity = null;
        }
    }
}
