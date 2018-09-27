#define _USE_MATH_DEFINES
#include <algorithm>
#include <cmath>
#include <cstdlib>
#include <cstring>

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

double* New(int x, int y)
{
	return Double(x * y, 0.0);
}

extern "C"
{
	static double* LensPhase = NULL;

	void Compute(int argc, void** argv)
	{
		if (argc >= 5)
		{
			auto srcx = *((int*)(argv[0]));
			auto srcy = *((int*)(argv[1]));
			auto Z = *((double*)(argv[2])) * 1e-6;
			auto wavelength = *((double*)(argv[3])) * 1e-9;
			auto pitch = *((double*)(argv[4])) * 1e-6;

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

			free(LensPhase);

			LensPhase = Double(srcx * srcy, 0.0);

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
		}
	}
	
	double* Phase()
	{
		return LensPhase;
	}
	
	void Release()
	{
		free(LensPhase);

		LensPhase = NULL;
	}
}
