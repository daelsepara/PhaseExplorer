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

double Mod(double a, double m)
{
	return a - m * floor(a / m);
}
	
extern "C"
{
	static double* MirrorPhase = NULL;

	void Compute(int argc, void** argv)
	{
		if (argc >= 4)
		{
			auto srcx = *((int*)(argv[0]));
			auto srcy = *((int*)(argv[1]));
			
			auto phiX = Double(srcx, 0.0);
			auto phiY = Double(srcy, 0.0);

			auto MirrorX = *((double*)(argv[2]));
			auto MirrorY = *((double*)(argv[3]));

			for (auto i = 0; i < srcx; i++)
			{
				phiX[i] = (double)i * MirrorX / 500;
			}

			for (auto i = 0; i < srcy; i++)
			{
				phiY[i] = (double)i * MirrorY / 500;
			}

			free(MirrorPhase);

			MirrorPhase = Double(srcx * srcy, 0.0);

			for (auto y = 0; y < srcy; y++)
			{
				for (auto x = 0; x < srcx; x++)
				{
					MirrorPhase[y * srcx + x] = Mod(phiX[x] + phiY[y], 2.0 * M_PI);
				}
			}

			free(phiX);
			free(phiY);
		}
	}
	
	double* Phase()
	{
		return MirrorPhase;
	}
	
	void Release()
	{
		free(MirrorPhase);

		MirrorPhase = NULL;
	}
}
