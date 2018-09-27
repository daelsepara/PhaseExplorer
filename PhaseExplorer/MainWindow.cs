using Gdk;
using GLib;
using Gtk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

public partial class MainWindow : Gtk.Window
{
	Pixbuf InputPixbuf, PhasePixbuf, ReconPixbuf;

	FileChooserDialog ImageSaver, ImageLoader;

	String FileName;

	Dialog Confirm;

	Mutex Processing = new Mutex();

	CultureInfo ci = new CultureInfo("en-us");

	bool ControlsEnabled;

	List<Point> Spots = new List<Point>();

	Stopwatch timer = new Stopwatch();

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();

		InitializeUserInterface();
	}

	protected void Tic()
	{
		timer.Restart();
	}

	protected long Ticks()
	{
		return timer.ElapsedMilliseconds;
	}

	protected FileFilter AddFilter(string name, params string[] patterns)
	{
		var filter = new FileFilter() { Name = name };

		foreach (var pattern in patterns)
			filter.AddPattern(pattern);

		return filter;
	}

	protected void DisableControls()
	{
		ControlsEnabled = false;
	}

	protected void EnableControls()
	{
		ControlsEnabled = true;
	}

	protected void InitializeUserInterface()
	{
		Title = "Phase Explorer";

		InputPixbuf = Common.InitializePixbuf(Parameters.SLModX, Parameters.SLModY);
		PhasePixbuf = Common.InitializePixbuf(Parameters.SLModX, Parameters.SLModY);
		ReconPixbuf = Common.InitializePixbuf(Parameters.SLModX, Parameters.SLModY);

		InputImage.Pixbuf = Common.InitializePixbuf(Parameters.WindowX, Parameters.WindowY);
		PhaseImage.Pixbuf = Common.InitializePixbuf(Parameters.WindowX, Parameters.WindowY);
		ReconImage.Pixbuf = Common.InitializePixbuf(Parameters.WindowX, Parameters.WindowY);

		ResetScrollBars();

		RenderImage(InputImage, InputPixbuf, Parameters.InputX, Parameters.InputY);
		RenderImage(PhaseImage, PhasePixbuf, Parameters.PhaseX, Parameters.PhaseY);
		RenderImage(ReconImage, ReconPixbuf, Parameters.ReconX, Parameters.ReconY);

		Confirm = new Dialog(
			"Are you sure?",
			this,
			DialogFlags.Modal,
			"Yes", ResponseType.Accept,
			"No", ResponseType.Cancel
		)
		{
			Resizable = false,
			KeepAbove = true,
			TypeHint = WindowTypeHint.Dialog,
			WidthRequest = 250
		};

		Confirm.ActionArea.LayoutStyle = ButtonBoxStyle.Center;
		Confirm.WindowStateEvent += OnWindowStateEvent;

		ImageSaver = new FileChooserDialog(
			"Save Filtered Image",
			this,
			FileChooserAction.Save,
			"Cancel", ResponseType.Cancel,
			"Save", ResponseType.Accept
		);

		ImageLoader = new FileChooserDialog(
			"Load Image",
			this,
			FileChooserAction.Save,
			"Cancel", ResponseType.Cancel,
			"Load", ResponseType.Accept
		);

		ImageSaver.AddFilter(AddFilter("png", "*.png"));
		ImageSaver.AddFilter(AddFilter("jpg", "*.jpg", "*.jpeg"));
		ImageSaver.AddFilter(AddFilter("tif", "*.tif", "*.tiff"));
		ImageSaver.AddFilter(AddFilter("bmp", "*.bmp"));
		ImageSaver.AddFilter(AddFilter("ico", "*.ico"));
		ImageSaver.Filter = ImageSaver.Filters[0];

		ImageLoader.AddFilter(AddFilter("Image files (png/jpg/jpeg/tif/tiff/bmp/gif/ico/xpm/icns/pgm)", "*.png", "*.jpg", "*.jpeg", "*.tif", "*.tiff", "*.bmp", "*.gif", "*.ico", "*.xpm", "*.icns", "*.pgm"));
		ImageLoader.Filter = ImageLoader.Filters[0];

		DisableControls();

		SetParameters();

		EnableControls();

		ToggleGerchbergSaxtonPhase();
		ToggleLensPhase();
		ToggleBlazedPhase();

		Idle.Add(new IdleHandler(OnIdle));
	}

	protected void ResetInputScrollBars()
	{
		InputScrollX.Value = 0;
		InputScrollY.Value = 0;

		InputScrollX.Sensitive = InputPixbuf.Width > Parameters.WindowX;
		InputScrollY.Sensitive = InputPixbuf.Height > Parameters.WindowY;

		InputScrollX.Visible = InputPixbuf.Width > Parameters.WindowX;
		InputScrollY.Visible = InputPixbuf.Height > Parameters.WindowY;

		if (InputScrollX.Sensitive)
		{
			InputScrollX.SetRange(0.0, InputPixbuf.Width - Parameters.WindowX);
		}
		else
		{
			InputScrollX.SetRange(0.0, Parameters.WindowX);
		}

		if (InputScrollY.Sensitive)
		{
			InputScrollY.SetRange(0.0, InputPixbuf.Height - Parameters.WindowY);
		}
		else
		{
			InputScrollY.SetRange(0.0, Parameters.WindowY);
		}

		Parameters.InputX = 0;
		Parameters.InputY = 0;
	}

	protected void ResetPhaseScrollBars()
	{
		PhaseScrollX.Value = 0;
		PhaseScrollY.Value = 0;

		PhaseScrollX.Sensitive = PhasePixbuf.Width > Parameters.WindowX;
		PhaseScrollY.Sensitive = PhasePixbuf.Height > Parameters.WindowY;

		PhaseScrollX.Visible = PhasePixbuf.Width > Parameters.WindowX;
		PhaseScrollY.Visible = PhasePixbuf.Height > Parameters.WindowY;

		if (PhaseScrollX.Sensitive)
		{
			PhaseScrollX.SetRange(0.0, PhasePixbuf.Width - Parameters.WindowX);
		}
		else
		{
			PhaseScrollX.SetRange(0.0, Parameters.WindowX);
		}

		if (PhaseScrollY.Sensitive)
		{
			PhaseScrollY.SetRange(0.0, PhasePixbuf.Height - Parameters.WindowY);
		}
		else
		{
			PhaseScrollY.SetRange(0.0, Parameters.WindowY);
		}

		Parameters.PhaseX = 0;
		Parameters.PhaseY = 0;
	}

	protected void ResetReconScrollBars()
	{
		ReconScrollX.Value = 0;
		ReconScrollY.Value = 0;

		ReconScrollX.Sensitive = ReconPixbuf.Width > Parameters.WindowX;
		ReconScrollY.Sensitive = ReconPixbuf.Height > Parameters.WindowY;

		ReconScrollX.Visible = ReconPixbuf.Width > Parameters.WindowX;
		ReconScrollY.Visible = ReconPixbuf.Height > Parameters.WindowY;

		if (ReconScrollX.Sensitive)
		{
			ReconScrollX.SetRange(0.0, ReconPixbuf.Width - Parameters.WindowX);
		}
		else
		{
			ReconScrollX.SetRange(0.0, Parameters.WindowX);
		}

		if (ReconScrollY.Sensitive)
		{
			ReconScrollY.SetRange(0.0, ReconPixbuf.Height - Parameters.WindowY);
		}
		else
		{
			ReconScrollY.SetRange(0.0, Parameters.WindowY);
		}

		Parameters.ReconX = 0;
		Parameters.ReconY = 0;
	}

	protected void ResetScrollBars()
	{
		ResetInputScrollBars();
		ResetPhaseScrollBars();
		ResetReconScrollBars();
	}

	protected void SetParameters()
	{
		Pitch.Value = Parameters.Pitch;
		Wavelength.Value = Parameters.Wavelength;

		BlazedX.Value = Parameters.BlazedX;
		BlazedY.Value = Parameters.BlazedY;

		BlazedScrollX.Value = Parameters.BlazedX;
		BlazedScrollY.Value = Parameters.BlazedY;

		LensZ.Value = Parameters.LensZ;
		LensZScroll.Value = Parameters.LensZ;

		NFFT.Value = Parameters.NFFT;

		PropagationZ.Value = Parameters.PropagationZ;
		PropagationScrollZ.Value = Parameters.PropagationZ;
		PropagationWavelength.Value = Parameters.Wavelength;
		PropagationPitch.Value = Parameters.Pitch;
	}

	protected void ToggleGerchbergSaxtonPhase()
	{
		Iterations.Sensitive = GerchbergSaxtonPhase.Active;
	}

	protected void ToggleLensPhase()
	{
		LensZ.Sensitive = LensPhase.Active;
		LensZScroll.Sensitive = LensPhase.Active;
	}

	protected void ToggleBlazedPhase()
	{
		BlazedX.Sensitive = BlazedPhase.Active;
		BlazedScrollX.Sensitive = BlazedPhase.Active;
		BlazedY.Sensitive = BlazedPhase.Active;
		BlazedScrollY.Sensitive = BlazedPhase.Active;
	}

	protected void CopyToImage(Gtk.Image image, Pixbuf pixbuf, int OriginX, int OriginY)
	{
		if (pixbuf != null && image.Pixbuf != null)
		{
			image.Pixbuf.Fill(0);

			pixbuf.CopyArea(OriginX, OriginY, Math.Min(Parameters.WindowX, pixbuf.Width), Math.Min(Parameters.WindowY, pixbuf.Height), image.Pixbuf, 0, 0);

			image.QueueDraw();
		}
	}

	protected void RenderImage(Gtk.Image image, Pixbuf pixbuf, int X, int Y)
	{
		CopyToImage(image, pixbuf, X, Y);
	}

	protected void DrawEllipse(Gdk.GC gc, Gdk.Window dest, int X, int Y, int a, int b, bool filled = false)
	{
		dest.DrawArc(gc, filled, X, Y, a, b, 0, 360 * 64);
	}

	protected void DrawSpot(Gtk.Image image, int x, int y)
	{
		var dest = image.GdkWindow;

		var gc = new Gdk.GC(dest)
		{
			RgbFgColor = new Color(255, 255, 255),
			RgbBgColor = new Color(255, 255, 255)
		};

		DrawEllipse(gc, dest, x, y, 16, 16, true);
	}

	protected void RenderSpots(Gtk.Image image, Pixbuf input, int OriginX, int OriginY)
	{
		CopyToImage(image, input, OriginX, OriginY);

		if (Spots.Count > 0)
		{
			foreach (var spot in Spots)
			{
				var x = Convert.ToInt32(spot.X) - OriginX;
				var y = Convert.ToInt32(spot.Y) - OriginY;

				if (x >= 0 && x < Parameters.WindowX && y >= 0 && y < Parameters.WindowY)
				{
					var pixbuf = image.Pixbuf;

					var ptr = pixbuf.Pixels + y * pixbuf.Rowstride + x * pixbuf.NChannels;

					for (var offset = 0; offset < pixbuf.NChannels; offset++)
					{
						Marshal.WriteByte(ptr, offset, 255);
					}
				}
			}

			image.QueueDraw();
		}
	}

	protected void UpdateSpotsList()
	{
		PointsView.Buffer.Clear();

		if (Spots.Count > 0)
		{
			var text = "";

			for (var i = 0; i < Spots.Count; i++)
			{
				var spot = Spots[i];

				if (i > 0)
					text += "\n";

				text += String.Format(ci, "({0}, {1})", spot.X, spot.Y);
			}

			PointsView.Buffer.Text = text.Trim();
		}
	}

	protected string GetFileName(string fullpath)
	{
		return System.IO.Path.GetFileNameWithoutExtension(fullpath);
	}

	protected string GetName(string fullpath)
	{
		return System.IO.Path.GetFileName(fullpath);
	}

	protected string GetDirectory(string fullpath)
	{
		return System.IO.Path.GetDirectoryName(fullpath);
	}

	protected void LoadImageFile(Gtk.Image image, ref Pixbuf dst, bool input = true)
	{
		ImageLoader.Title = input ? "Load target pattern" : "Load phase pattern";

		// Add most recent directory
		if (!string.IsNullOrEmpty(ImageLoader.Filename))
		{
			var directory = GetDirectory(ImageLoader.Filename);

			if (Directory.Exists(directory))
			{
				ImageLoader.SetCurrentFolder(directory);
			}
		}

		if (ImageLoader.Run() == (int)ResponseType.Accept)
		{
			if (!string.IsNullOrEmpty(ImageLoader.Filename))
			{
				FileName = ImageLoader.Filename;

				try
				{
					var temp = new Pixbuf(FileName);

					if (dst != null && temp != null)
					{
						Common.Free(dst);

						dst = Common.InitializePixbuf(temp.Width, temp.Height);

						temp.Composite(dst, 0, 0, temp.Width, temp.Height, 0, 0, 1, 1, InterpType.Nearest, 255);

						if (input)
						{
							ResetInputScrollBars();
						}
						else
						{
							ResetPhaseScrollBars();
						}

						RenderImage(image, dst, input ? Parameters.InputX : Parameters.PhaseX, input ? Parameters.InputY : Parameters.PhaseY);
					}

					Common.Free(temp);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error: {0}", ex.Message);
				}
			}
		}

		ImageLoader.Hide();
	}

	protected void SaveImageFile(Pixbuf src, bool input = true, bool recon = false)
	{
		ImageSaver.Title = input ? "Save target pattern" : (recon ? "Save reconstruction" : "Save phase pattern");

		string directory;

		// Add most recent directory
		if (!string.IsNullOrEmpty(ImageSaver.Filename))
		{
			directory = GetDirectory(ImageSaver.Filename);

			if (Directory.Exists(directory))
			{
				ImageSaver.SetCurrentFolder(directory);
			}
		}

		if (ImageSaver.Run() == (int)ResponseType.Accept)
		{
			if (!string.IsNullOrEmpty(ImageSaver.Filename))
			{
				FileName = ImageSaver.Filename;

				directory = GetDirectory(FileName);

				var ext = ImageSaver.Filter.Name;

				var fmt = ext;

				switch (ext)
				{
					case "jpg":

						if (!FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) && !FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
						{
							FileName = String.Format("{0}.jpg", GetFileName(FileName));
						}

						fmt = "jpeg";

						break;

					case "tif":

						if (!FileName.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) && !FileName.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase))
						{
							FileName = String.Format("{0}.tif", GetFileName(FileName));
						}

						fmt = "tiff";

						break;

					default:

						FileName = String.Format("{0}.{1}", GetFileName(FileName), ext);

						break;
				}

				if (src != null)
				{
					FileName = GetName(FileName);

					var fullpath = String.Format("{0}/{1}", directory, FileName);

					try
					{
						src.Save(fullpath, fmt);

						FileName = fullpath;
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error saving {0}: {1}", FileName, ex.Message);
					}
				}
			}
		}

		ImageSaver.Hide();
	}

	unsafe protected void ComputePhase()
	{
		Tic();

		var start = Ticks();

		var srcx = InputPixbuf.Width;
		var srcy = InputPixbuf.Height;

		var size = srcx * srcy;

		var phase = new PhaseOutput(size);

		if (Spots.Count > 0)
		{
			SpotsDLL.SetParameters(srcx, srcy, Parameters.NFFT, Spots, true);

			var spots = SpotsDLL.ComputePhase("spots");

			phase.Add(spots, size);
		}

		if (GerchbergSaxtonPhase.Active)
		{
			GerchbergSaxtonDLL.SetIterations(Convert.ToInt32(Iterations.Value));
			GerchbergSaxtonDLL.SetTarget(InputPixbuf);

			var gs = GerchbergSaxtonDLL.ComputePhase("gs");

			phase.Add(gs, size);
		}

		if (LensPhase.Active)
		{
			LensPhaseDLL.SetParameters(srcx, srcy, Parameters.LensZ, Parameters.Pitch, Parameters.Wavelength);

			var lens = LensPhaseDLL.ComputePhase("lens");

			phase.Add(lens, size);

			lens.Free();
		}

		if (BlazedPhase.Active)
		{
			BlazedPhaseDLL.SetParameters(srcx, srcy, Parameters.BlazedX, Parameters.BlazedY);

			var blazed = BlazedPhaseDLL.ComputePhase("blazed");

			phase.Add(blazed, size);

			blazed.Free();
		}

		var pixbuf = Common.PreparePixbuf(phase.Phase, srcx, srcy);

		if (PhasePixbuf != null && pixbuf != null)
		{
			Common.Free(PhasePixbuf);

			PhasePixbuf = Common.InitializePixbuf(pixbuf.Width, pixbuf.Height);

			pixbuf.Composite(PhasePixbuf, 0, 0, pixbuf.Width, pixbuf.Height, 0, 0, 1, 1, InterpType.Nearest, 255);

			ResetPhaseScrollBars();

			RenderImage(PhaseImage, PhasePixbuf, Parameters.PhaseX, Parameters.PhaseY);
		}

		phase.Free();

		Common.Free(pixbuf);

		var elapsed = Ticks() - start;

		Elapsed.Text = elapsed.ToString(ci);
		PhaseElapsed.Text = elapsed.ToString(ci);
	}

	unsafe protected void ComputePropagation()
	{
		Tic();

		var start = Ticks();

		var srcx = PhasePixbuf.Width;
		var srcy = PhasePixbuf.Height;

		var size = srcx * srcy;

		ReconDLL.SetParameters(Parameters.PropagationZ, Parameters.Wavelength, Parameters.Pitch);
		ReconDLL.SetTarget(PhasePixbuf);

		var intensity = ReconDLL.ComputeIntensity("recon");

		var pixbuf = Common.Intensity(intensity.Intensity, srcx, srcy);

		if (ReconPixbuf != null && pixbuf != null)
		{
			Common.Free(ReconPixbuf);

			ReconPixbuf = Common.InitializePixbuf(pixbuf.Width, pixbuf.Height);

			pixbuf.Composite(ReconPixbuf, 0, 0, pixbuf.Width, pixbuf.Height, 0, 0, 1, 1, InterpType.Nearest, 255);

			ResetReconScrollBars();

			RenderImage(ReconImage, ReconPixbuf, Parameters.PhaseX, Parameters.PhaseY);
		}

		intensity.Free();

		Common.Free(pixbuf);

		var elapsed = Ticks() - start;

		Elapsed.Text = elapsed.ToString(ci);
		ReconElapsed.Text = elapsed.ToString(ci);
	}

	protected bool GetConfirmation()
	{
		var confirm = Confirm.Run() == (int)ResponseType.Accept;

		Confirm.Hide();

		return confirm;
	}

	protected void CleanShutdown()
	{
		// Clean-Up Routines Here
		GerchbergSaxtonDLL.Free();

		Common.Free(InputPixbuf, PhasePixbuf, ReconPixbuf, InputImage.Pixbuf, PhaseImage.Pixbuf, ReconImage.Pixbuf);
	}

	protected void Quit()
	{
		CleanShutdown();

		Application.Quit();
	}

	protected void OnWindowStateEvent(object sender, WindowStateEventArgs args)
	{
		var state = args.Event.NewWindowState;

		if (state == WindowState.Iconified)
		{
			Confirm.Hide();
		}

		args.RetVal = true;
	}

	void OnQuitButtonClicked(object sender, EventArgs args)
	{
		OnDeleteEvent(sender, new DeleteEventArgs());
	}

	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		if (GetConfirmation())
		{
			Quit();
		}

		a.RetVal = true;
	}

	bool OnIdle()
	{
		var wait = Processing.WaitOne();

		if (wait)
		{
			Processing.ReleaseMutex();
		}

		return true;
	}

	protected void OnOpenInputButtonClicked(object sender, EventArgs args)
	{
		LoadImageFile(InputImage, ref InputPixbuf, true);

		RenderSpots(InputImage, InputPixbuf, Parameters.InputX, Parameters.InputY);
	}

	protected void OnSaveInputButtonClicked(object sender, EventArgs args)
	{
		SaveImageFile(InputPixbuf, true);
	}

	protected void OnInputScrollYValueChanged(object sender, EventArgs e)
	{
		Parameters.InputY = Convert.ToInt32(InputScrollY.Value);

		RenderSpots(InputImage, InputPixbuf, Parameters.InputX, Parameters.InputY);
	}

	protected void OnInputScrollXValueChanged(object sender, EventArgs e)
	{
		Parameters.InputX = Convert.ToInt32(InputScrollX.Value);

		RenderSpots(InputImage, InputPixbuf, Parameters.InputX, Parameters.InputY);
	}

	protected void OnOpenPhaseButtonClicked(object sender, EventArgs e)
	{
		LoadImageFile(PhaseImage, ref PhasePixbuf, false);
	}

	protected void OnComputePhaseButtonClicked(object sender, EventArgs e)
	{
		ComputePhase();
	}

	protected void OnSavePhaseButtonClicked(object sender, EventArgs e)
	{
		SaveImageFile(PhasePixbuf, false);
	}

	protected void OnPhaseScrollYValueChanged(object sender, EventArgs e)
	{
		Parameters.PhaseY = Convert.ToInt32(PhaseScrollY.Value);

		RenderImage(PhaseImage, PhasePixbuf, Parameters.PhaseX, Parameters.PhaseY);
	}

	protected void OnPhaseScrollXValueChanged(object sender, EventArgs e)
	{
		Parameters.PhaseX = Convert.ToInt32(PhaseScrollX.Value);

		RenderImage(PhaseImage, PhasePixbuf, Parameters.PhaseX, Parameters.PhaseY);
	}

	protected void OnLensZScrollValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.LensZ = Convert.ToDouble(LensZScroll.Value);

		LensZ.Value = Parameters.LensZ;

		EnableControls();
	}

	protected void OnLensZValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.LensZ = Convert.ToDouble(LensZ.Value);

		LensZScroll.Value = Parameters.LensZ;

		EnableControls();
	}

	protected void OnLensPhaseToggled(object sender, EventArgs e)
	{
		ToggleLensPhase();
	}

	protected void OnGerchbergSaxtonPhaseToggled(object sender, EventArgs e)
	{
		ToggleGerchbergSaxtonPhase();
	}

	protected void OnBlazedPhaseToggled(object sender, EventArgs e)
	{
		ToggleBlazedPhase();
	}

	protected void OnBlazedXValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.BlazedX = Convert.ToDouble(BlazedX.Value);

		BlazedScrollX.Value = Parameters.BlazedX;

		EnableControls();
	}

	protected void OnBlazedScrollXValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.BlazedX = Convert.ToDouble(BlazedScrollX.Value);

		BlazedX.Value = Parameters.BlazedX;

		EnableControls();
	}

	protected void OnBlazedYValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.BlazedY = Convert.ToDouble(BlazedY.Value);

		BlazedScrollY.Value = Parameters.BlazedY;

		EnableControls();
	}

	protected void OnBlazedScrollYValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.BlazedY = Convert.ToDouble(BlazedScrollY.Value);

		BlazedY.Value = Parameters.BlazedY;

		EnableControls();
	}

	protected void OnInputEventBoxButtonPressEvent(object sender, ButtonPressEventArgs args)
	{
		if (AddPointsButton.Active && args.Event.Button == 1)
		{
			var x = Convert.ToInt32(args.Event.X) + Parameters.InputX;
			var y = Convert.ToInt32(args.Event.Y) + Parameters.InputY;

			Spots.Add(new Point(x, y));

			UpdateSpotsList();

			RenderSpots(InputImage, InputPixbuf, Parameters.InputX, Parameters.InputY);
		}
	}

	protected void OnDeletePointsButtonClicked(object sender, EventArgs args)
	{
		if (!ControlsEnabled)
			return;

		Spots.Clear();

		UpdateSpotsList();

		RenderSpots(InputImage, InputPixbuf, Parameters.InputX, Parameters.InputY);
	}

	void OnRemovePointButtonClicked(object sender, EventArgs args)
	{
		PointsView.Buffer.GetSelectionBounds(out TextIter start, out TextIter end);

		var queue = new List<Point>();

		if (start.Line >= 0 && start.Line < Spots.Count && end.Line >= 0 && end.Line < Spots.Count)
		{
			for (var i = 0; i < Spots.Count; i++)
			{
				if (i < start.Line || i > end.Line)
					queue.Add(Spots[i]);
			}

			Spots.Clear();

			Spots.AddRange(queue);

			UpdateSpotsList();

			RenderSpots(InputImage, InputPixbuf, Parameters.InputX, Parameters.InputY);
		}
	}

	protected void OnNFFTValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		Parameters.NFFT = Convert.ToInt32(NFFT.Value);
	}

	protected void OnPropagationZValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.PropagationZ = Convert.ToDouble(PropagationZ.Value);

		PropagationScrollZ.Value = Parameters.PropagationZ;

		EnableControls();
	}

	protected void OnPropagationScrollZValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.PropagationZ = Convert.ToDouble(PropagationScrollZ.Value);

		PropagationZ.Value = Parameters.PropagationZ;

		EnableControls();
	}

	protected void OnReconScrollYValueChanged(object sender, EventArgs e)
	{
		Parameters.ReconY = Convert.ToInt32(ReconScrollY.Value);

		RenderImage(ReconImage, ReconPixbuf, Parameters.ReconX, Parameters.ReconY);
	}

	protected void OnReconScrollXValueChanged(object sender, EventArgs e)
	{
		Parameters.ReconX = Convert.ToInt32(ReconScrollX.Value);

		RenderImage(ReconImage, ReconPixbuf, Parameters.ReconX, Parameters.ReconY);
	}

	protected void OnPropagationWavelengthValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.Wavelength = Convert.ToDouble(PropagationWavelength.Value);

		Wavelength.Value = Parameters.Wavelength;

		EnableControls();
	}

	protected void OnComputePropagationButtonClicked(object sender, EventArgs e)
	{
		ComputePropagation();
	}

	protected void OnPropagationPitchValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.Pitch = Convert.ToDouble(PropagationPitch.Value);

		Pitch.Value = Parameters.Pitch;

		EnableControls();
	}

	protected void OnSaveReconstructionButtonClicked(object sender, EventArgs e)
	{
		SaveImageFile(ReconPixbuf, false, true);
	}

	protected void OnPitchValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.Pitch = Convert.ToDouble(Pitch.Value);

		PropagationPitch.Value = Parameters.Pitch;

		EnableControls();
	}

	protected void OnWavelengthValueChanged(object sender, EventArgs e)
	{
		if (!ControlsEnabled)
			return;

		DisableControls();

		Parameters.Wavelength = Convert.ToDouble(Wavelength.Value);

		PropagationWavelength.Value = Parameters.Wavelength;

		EnableControls();
	}
}
