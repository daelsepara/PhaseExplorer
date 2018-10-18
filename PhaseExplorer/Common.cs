using Gdk;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

public static class Common
{
    // see: https://github.com/jpobst/Pinta/blob/1.6/Pinta.Core/Managers/SystemManager.cs#L125
    public static class OSTest
    {
        [DllImport("libc", EntryPoint = "uname")]
        static extern int Uname(IntPtr buf);

        public static bool IsWindows()
        {
            var isWindows = false;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    isWindows = true;

                    break;
            }

            return isWindows;
        }

        public static bool IsRunningOnMac()
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                buf = Marshal.AllocHGlobal(8192);
                // This is a hacktastic way of getting sysname from uname ()
                if (Uname(buf) == 0)
                {
                    string os = Marshal.PtrToStringAnsi(buf);

                    if (os == "Darwin")
                        return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                if (buf != IntPtr.Zero)
                    Marshal.FreeHGlobal(buf);
            }

            return false;
        }
    }

    public static Pixbuf InitializePixbuf(int width, int height)
    {
        var pixbuf = new Pixbuf(Colorspace.Rgb, false, 8, width, height);

        pixbuf.Fill(0);

        return pixbuf;
    }

    unsafe public static byte* PreparePixbuf(Pixbuf input)
    {
        var src = InitializePixbuf(input.Width, input.Height);

        input.Composite(src, 0, 0, input.Width, input.Height, 0, 0, 1, 1, InterpType.Nearest, 255);

        var Channels = 3;

        var temp = (byte*)Marshal.AllocHGlobal(src.Width * src.Height * Channels);

        for (var y = 0; y < src.Height; y++)
        {
            for (var x = 0; x < src.Width; x++)
            {
                var ptr = src.Pixels + y * src.Rowstride + x * src.NChannels;

                for (var offset = 0; offset < src.NChannels; offset++)
                {
                    temp[(y * src.Width + x) * Channels + offset] = Marshal.ReadByte(ptr, offset);
                }
            }
        }

        Free(src);

        return temp;
    }

    unsafe public static Pixbuf PreparePixbuf(double* src, int srcx, int srcy)
    {
        var dst = InitializePixbuf(srcx, srcy);

        for (var y = 0; y < dst.Height; y++)
        {
            for (var x = 0; x < dst.Width; x++)
            {
                var ptr = dst.Pixels + y * dst.Rowstride + x * dst.NChannels;
                var c = (byte)(255 * src[y * srcx + x] / (2 * Math.PI));

                for (var offset = 0; offset < dst.NChannels; offset++)
                {
                    Marshal.WriteByte(ptr, offset, c);
                }
            }
        }

        return dst;
    }

    unsafe public static Pixbuf Intensity(double* src, int srcx, int srcy)
    {
        var dst = InitializePixbuf(srcx, srcy);

        for (var y = 0; y < dst.Height; y++)
        {
            for (var x = 0; x < dst.Width; x++)
            {
                var ptr = dst.Pixels + y * dst.Rowstride + x * dst.NChannels;
                var c = (byte)(255 * src[y * srcx + x]);

                for (var offset = 0; offset < dst.NChannels; offset++)
                {
                    Marshal.WriteByte(ptr, offset, c);
                }
            }
        }

        return dst;
    }

    unsafe public static void Copy(Pixbuf dst, byte* src)
    {
        if (dst != null)
        {
            for (var y = 0; y < dst.Height; y++)
            {
                for (var x = 0; x < dst.Width; x++)
                {
                    var ptr = dst.Pixels + y * dst.Rowstride + x * dst.NChannels;

                    for (var offset = 0; offset < dst.NChannels; offset++)
                    {
                        Marshal.WriteByte(ptr, offset, src[(y * dst.Width + x) * dst.NChannels + offset]);
                    }
                }
            }
        }
    }

    public static CultureInfo ci = new CultureInfo("en-US");

    // see: https://www.johndcook.com/blog/csharp_erf/
    public static double Erf(double x)
    {
        // constants
        double a1 = Convert.ToDouble("0.254829592", ci);
        double a2 = Convert.ToDouble("-0.284496736", ci);
        double a3 = Convert.ToDouble("1.421413741", ci);
        double a4 = Convert.ToDouble("-1.453152027", ci);
        double a5 = Convert.ToDouble("1.061405429", ci);
        double p = Convert.ToDouble("0.3275911", ci);

        // Save the sign of x
        int sign = 1;
        if (x < 0)
            sign = -1;
        x = Math.Abs(x);

        // A&S formula 7.1.26
        double t = 1 / (1 + p * x);
        double y = 1 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

        return sign * y;
    }

    public static double Mod(double a, double m)
    {
        return a - m * Math.Floor(a / m);
    }

    public static void Free(params IDisposable[] trash)
    {
        foreach (var item in trash)
        {
            if (item != null)
            {
                item.Dispose();
            }
        }
    }

    unsafe public static void Free(params byte*[] trash)
    {
        foreach (var item in trash)
        {
            if (item != null)
            {
                Marshal.FreeHGlobal((IntPtr)item);
            }
        }
    }
}
