#define _USE_MATH_DEFINES
#include <algorithm>
#include <cmath>
#include <cstdlib>
#include <cstring>
#include <random>
#include <chrono>

#include "common.hpp"

#undef max

class RandomCpp
{
public:

	std::mt19937_64 generator;
	std::normal_distribution<double> normalDistribution;
	std::uniform_real_distribution<double> uniformDistribution;
	std::uniform_int_distribution<int> uniformIntDistribution;

	RandomCpp()
	{
		generator = std::mt19937_64(std::chrono::system_clock::now().time_since_epoch().count());
	}

	RandomCpp(int seed)
	{
		generator = std::mt19937_64(seed);
	}

	void UniformDistribution(double a, double b)
	{
		uniformDistribution = std::uniform_real_distribution<double>(a, b);
	}

	void UniformDistribution()
	{
		UniformDistribution(0.0, 1.0);
	}

	double NextDouble()
	{
		return uniformDistribution(generator);
	}

	void UniformIntDistribution(int a, int b)
	{
		uniformIntDistribution = std::uniform_int_distribution<int>(a, b);
	}

	void UniformIntDistribution()
	{
		UniformIntDistribution(0, std::numeric_limits<int>::max());
	}

	int NextInt()
	{
		return uniformIntDistribution(generator);
	}

	void NormalDistribution(double mean, double std)
	{
		normalDistribution = std::normal_distribution<double>(mean, std);
	}

	void NormalDistribution()
	{
		NormalDistribution(0.0, 1.0);
	}

	double NextNormal()
	{
		return normalDistribution(generator);
	}
};

double* Double(int Length, double val = 0.0)
{
	auto buffer = (double*)malloc(Length * sizeof(double));

	for (auto i = 0; i < Length; i++)
	{
		buffer[i] = val;
	}

	return buffer;
}

int* Int(int Length, int val = 0)
{
	auto arr = (int*)malloc(Length * sizeof(int));

	if (arr != NULL)
	{
		for (auto x = 0; x < Length; x++)
		{
			arr[x] = val;
		}
	}

	return arr;
}
	
double Mod(double a, double m)
{
	return a - m * floor(a / m);
}
	
extern "C"
{
	static double* MultiSpotPhase = NULL;

	void Compute(int argc, void** argv)
	{
		if (argc >= 6)
		{
			auto srcx = *((int*)(argv[0]));  // srcx
			auto srcy = *((int*)(argv[1]));  // srcy
			auto nfft = *((int*)(argv[2]));
			auto N = *((int*)(argv[3]));
			auto NX = ((double*)(argv[4]));
			auto NY = ((double*)(argv[5]));
			auto Nxy = nfft * nfft;

			auto mask = Int(Nxy);
			auto phase = Double(Nxy);

			for (auto i = 0; i < Nxy; i++)
			{
				mask[i] = (int)Mod((double)i, (double)N) + 1;
			}

			auto random = RandomCpp();

			for (auto rounds = 0; rounds < 3; rounds++)
			{
				for (auto i = 0; i < Nxy; i++)
				{
					random.UniformIntDistribution(i, Nxy);

					auto src = random.NextInt();
					auto x = mask[i];

					mask[i] = mask[src];
					mask[src] = x;
				}
			}

			auto Cn = nfft / 2;

			auto factor = 2.0 * M_PI / ((double)nfft);

			for (auto ii = 0; ii < nfft; ii++)
			{
				for (auto jj = 0; jj < nfft; jj++)
				{
					auto indexi = ii * nfft + jj;
					auto indexj = jj * nfft + ii;
					auto ci = (double)(ii - Cn);

					for (auto i = 0; i < N; i++)
					{
						phase[indexi] += factor * NY[i] * (mask[indexi] == (i + 1) ? 1.0 : 0.0) * ci;
						phase[indexj] += factor * NX[i] * (mask[indexj] == (i + 1) ? 1.0 : 0.0) * ci;
					}
				}
			}

			free(MultiSpotPhase);

			MultiSpotPhase = Double(srcx * srcy);

			auto sx = (nfft > srcx ? (nfft - srcx) / 2 : 0);
			auto sy = (nfft > srcy ? (nfft - srcy) / 2 : 0);
			auto dx = (srcx > nfft ? (srcx - nfft) / 2 : 0);
			auto dy = (srcy > nfft ? (srcy - nfft) / 2 : 0);

			auto xx = std::min(nfft, srcx);
			auto yy = std::min(nfft, srcy);
			
			for (auto y = 0; y < yy; y++)
			{
				for (auto x = 0; x < xx; x++)
				{
					auto src = (y + sy) * nfft + (x + sx);
					auto dst = (y + dy) * srcx + (x + dx);

					MultiSpotPhase[dst] = Mod(phase[src] + M_PI, 2.0 * M_PI);
				}
			}

			free(phase);

			free(mask);
		}
	}
	
	double* Phase()
	{
		return MultiSpotPhase;
	}
	
	void Release()
	{
		free(MultiSpotPhase);

		MultiSpotPhase = NULL;
	}
}
