#define _USE_MATH_DEFINES
#include <algorithm>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <fftw3.h>

#include "common.hpp"

double* Double(int Length, double val = 0.0)
{
	auto buffer = (double*)malloc(Length * sizeof(double));

	for (auto i = 0; i < Length; i++)
	{
		buffer[i] = val;
	}

	return buffer;
}

const int Re = 0;
const int Im = 1;

fftw_complex* Complex(int Length, double val = 0.0)
{
	auto dbl = (fftw_complex*)fftw_malloc(Length * sizeof(fftw_complex));

	if (dbl != NULL)
	{
		for (auto x = 0; x < Length; x++)
		{
			dbl[x][Re] = val;
			dbl[x][Im] = 0.0;
		}
	}

	return dbl;
}

fftw_complex* Copy(unsigned char* input, int srcx, int srcy)
{
	auto cmplx = (fftw_complex*)fftw_malloc(srcx * srcy * sizeof(fftw_complex));

	if (cmplx != NULL)
	{
		for (auto y = 0; y < srcy; y++)
		{
			for (auto x = 0; x < srcx; x++)
			{
				auto index = y * srcx + x;
				auto pixel = Luminance(CLR(input, srcx, srcy, x, y));
				auto phase = (pixel / 255.0) * 2.0 * M_PI; 
				
				cmplx[index][Re] = std::cos(phase);
				cmplx[index][Im] = std::sin(phase);
			}
		}
	}

	return cmplx;
}

void Shift(fftw_complex* complex, int sizex, int sizey)
{
	fftw_complex temp;
	auto midx = sizex / 2;
	auto midy = sizey / 2;

	for (auto y = 0; y < midy; y++)
	{
		for (auto x = 0; x < midx; x++)
		{
			// Exchange 1st and 4th quadrant
			temp[Re] = complex[y * sizex + x][Re];
			complex[y * sizex + x][Re] = complex[(midy + y) * sizex + (midx + x)][Re];
			complex[(midy + y) * sizex + (midx + x)][Re] = temp[Re];

			temp[Im] = complex[y * sizex + x][Im];
			complex[y * sizex + x][Im] = complex[(midy + y) * sizex + (midx + x)][Im];
			complex[(midy + y) * sizex + (midx + x)][Im] = temp[Im];

			// Exchange 2nd and 3rd quadrant
			temp[Re] = complex[y * sizex + (midx + x)][Re];
			complex[y * sizex + (midx + x)][Re] = complex[(midy + y) * sizex + x][Re];
			complex[(midy + y) * sizex + x][Re] = temp[Re];

			temp[Im] = complex[y * sizex + (midx + x)][Im];
			complex[y * sizex + (midx + x)][Im] = complex[(midy + y) * sizex + x][Im];
			complex[(midy + y) * sizex + x][Im] = temp[Im];
		}
	}
}

void Multiply(fftw_complex& product, fftw_complex A, fftw_complex B)
{
	product[Re] = A[Re] * B[Re] - A[Im] * B[Im];
	product[Im] = A[Re] * B[Im] + A[Im] * B[Re];
}

double Magnitude(fftw_complex complex)
{
	return sqrt(complex[Re] * complex[Re] + complex[Im] * complex[Im]);
}

static void Normalize(double*& image, int sizex, int sizey)
{
	auto min = std::numeric_limits<double>::max();
	auto max = std::numeric_limits<double>::min();

	for (auto i = 0; i < sizex * sizey; i++)
	{
		if (image[i] > max)
			max = image[i];

		if (image[i] < min)
			min = image[i];
	}

	double diff = std::abs(max - min);

	if (diff > 0.0)
	{
		for (auto i = 0; i < sizex * sizey; i++)
		{
			image[i] = (image[i] - min) / diff;
		}
	}
}
					
extern "C"
{
	static double* Reconstruction = NULL;

	void Compute(int argc, void** argv)
	{
		if (argc >= 6)
		{
			auto Input = ((unsigned char*)(argv[0]));
			auto srcx = *((int*)(argv[1]));
			auto srcy = *((int*)(argv[2]));
			auto Z = *((double*)(argv[3])) * 1e-6;
			auto wavelength = *((double*)(argv[4])) * 1e-9;
			auto pitch = *((double*)(argv[5])) * 1e-6;

			auto Nlambda = wavelength / (pitch * pitch);
			auto K0 = 2 * M_PI / wavelength;
			auto KK0 = K0 * K0;
			auto srcx_2 = srcx / 2;
			auto srcy_2 = srcy / 2;

			auto kkx = Double(srcx, 0.0);
			auto kky = Double(srcy, 0.0);

			for (auto i = 0; i < srcx; i++)
			{
				kkx[i] = pow((double)(i - srcx_2) * Nlambda, 2.0);
			}

			for (auto i = 0; i < srcy; i++)
			{
				kky[i] = pow((double)(i - srcy_2) * Nlambda, 2.0);
			}

			auto LensPhase = Double(srcx * srcy, 0.0);

			for (auto y = 0; y < srcy; y++)
			{
				for (auto x = 0; x < srcx; x++)
				{
					auto karg = kkx[x] + kky[y];

					if (karg <= KK0)
					{
						LensPhase[y * srcx + x] = sqrt(KK0 - karg) * Z;
					}
				}
			}

			free(kkx);
			free(kky);
			
			auto Output = Copy(Input, srcx, srcy);
			
			for (auto i = 0; i < srcx * srcy; i++)
			{
				auto kz = LensPhase[i];

				fftw_complex amp = { Output[i][Re] , Output[i][Im] };
				fftw_complex complex = { cos(kz) , -sin(kz)};

				Multiply(Output[i], amp, complex);
			}
			
			auto complex = Complex(srcx * srcy);
			auto dtemp = Complex(srcx * srcy);
			
			auto reconplan = fftw_plan_dft_2d(srcy, srcx, dtemp, complex, FFTW_FORWARD, FFTW_ESTIMATE);
			
			Shift(Output, srcx, srcy);

			fftw_execute_dft(reconplan, Output, complex);

			Shift(complex, srcx, srcy);

			free(Reconstruction);
			Reconstruction = Double(srcx * srcy);
			
			for (auto x = 0; x < srcx * srcy; x++)
			{
				auto val = Magnitude(complex[x]);

				Reconstruction[x] = val;
			}

			Normalize(Reconstruction, srcx, srcy);

			fftw_cleanup();
			
			fftw_free(dtemp);
			fftw_free(complex);
			free(LensPhase);
		}
	}
	
	double* Intensity()
	{
		return Reconstruction;
	}
	
	void Release()
	{
		free(Reconstruction);

		Reconstruction = NULL;
	}
}
