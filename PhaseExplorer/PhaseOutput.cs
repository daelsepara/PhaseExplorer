using System;
using System.Runtime.InteropServices;

public class PhaseOutput
{
    unsafe public double* Phase;

    unsafe public PhaseOutput(int length)
    {
        Phase = (double*)Marshal.AllocHGlobal(length * sizeof(double));

        for (var i = 0; i < length; i++)
            Phase[i] = 0;
    }

    unsafe public PhaseOutput(double* phase, int length)
    {
        Phase = (double*)Marshal.AllocHGlobal(length * sizeof(double));

        for (var i = 0; i < length; i++)
            Phase[i] = phase[i];
    }

    unsafe public void Add(PhaseOutput phase, int length)
    {
        for (var i = 0; i < length; i++)
        {
            Phase[i] += phase.Phase[i];
            Phase[i] = Common.Mod(Phase[i], 2 * Math.PI);
        }
    }

    unsafe public void Free()
    {
        if (Phase != null)
        {
            Marshal.FreeHGlobal((IntPtr)Phase);

            Phase = null;
        }
    }
}
