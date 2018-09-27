using Gdk;
using System;
using System.IO;
using System.Runtime.InteropServices;

public static class GerchbergSaxtonDLL
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	unsafe delegate double* FPhase();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate void FRelease();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	unsafe delegate void FCompute(int argc, void** argv);

	static Pixbuf Target;
	static int Iterations;

	public static void SetTarget(Pixbuf input)
	{
		if (input != null)
		{
			Common.Free(Target);

			Target = Common.InitializePixbuf(input.Width, input.Height);

			input.Composite(Target, 0, 0, input.Width, input.Height, 0, 0, 1, 1, InterpType.Nearest, 255);
		}
	}

	public static void SetIterations(int iterations)
	{
		Iterations = iterations;
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

			var target = Common.PreparePixbuf(Target);
			var targetx = Target.Width;
			var targety = Target.Height;

			var iterations = Iterations;

			void** Parameters = stackalloc void*[4];

			Parameters[0] = target;
			Parameters[1] = &targetx;
			Parameters[2] = &targety;
			Parameters[3] = &iterations;

			Compute(4, Parameters);

			var output = Phase();

			var phase = new PhaseOutput(output, targetx * targety);

			// Free resources
			Release();

			DLLLoader.FreeLibrary(pLibrary);

			return phase;
		}

		var length = Target != null ? Target.Width * Target.Height : 256 * 256;

		return new PhaseOutput(length);
	}

	public static void Free()
	{
		Common.Free(Target);
	}
}
