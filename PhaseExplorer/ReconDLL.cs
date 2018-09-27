using Gdk;
using System;
using System.IO;
using System.Runtime.InteropServices;

public static class ReconDLL
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	unsafe delegate double* FIntensity();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate void FRelease();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	unsafe delegate void FCompute(int argc, void** argv);

	static Pixbuf Target;
	static double Z;
	static double Wavelength;
	static double Pitch;

	public static void SetTarget(Pixbuf input)
	{
		if (input != null)
		{
			Common.Free(Target);

			Target = Common.InitializePixbuf(input.Width, input.Height);

			input.Composite(Target, 0, 0, input.Width, input.Height, 0, 0, 1, 1, InterpType.Nearest, 255);
		}
	}

	public static void SetParameters(double z, double wavelength, double pitch)
	{
		Z = z;
		Wavelength = wavelength;
		Pitch = pitch;
	}

	unsafe public static IntensityOutput ComputeIntensity(string dll)
	{
		var file = Common.OSTest.IsWindows() ? String.Format("./libphase++{0}.dll", dll) : (Common.OSTest.IsRunningOnMac() ? String.Format("./libphase++{0}.dylib", dll) : String.Format("./libphase++{0}.so", dll));

		if (File.Exists(file))
		{
			IntPtr pLibrary = DLLLoader.LoadLibrary(file);
			IntPtr pCompute = DLLLoader.GetProcAddress(pLibrary, "Compute");
			IntPtr pIntensity = DLLLoader.GetProcAddress(pLibrary, "Intensity");
			IntPtr pRelease = DLLLoader.GetProcAddress(pLibrary, "Release");

			var Compute = DLLLoader.LoadFunction<FCompute>(pCompute);
			var Intensity = DLLLoader.LoadFunction<FIntensity>(pIntensity);
			var Release = DLLLoader.LoadFunction<FRelease>(pRelease);

			var target = Common.PreparePixbuf(Target);
			var targetx = Target.Width;
			var targety = Target.Height;

			var z = Z;
			var wavelength = Wavelength;
			var pitch = Pitch;

			void** Parameters = stackalloc void*[6];

			Parameters[0] = target;
			Parameters[1] = &targetx;
			Parameters[2] = &targety;
			Parameters[3] = &z;
			Parameters[4] = &wavelength;
			Parameters[5] = &pitch;

			Compute(6, Parameters);

			var output = Intensity();

			var intensity = new IntensityOutput(output, targetx * targety);

			// Free resources
			Release();

			DLLLoader.FreeLibrary(pLibrary);

			return intensity;
		}

		var length = Target != null ? Target.Width * Target.Height : 256 * 256;

		return new IntensityOutput(length);
	}

	public static void Free()
	{
		Common.Free(Target);
	}
}
