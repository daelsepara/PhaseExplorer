#pragma once

#include <cstdlib>
#include <cstring>
#include <algorithm>

int CLR(unsigned char* Input, int srcx, int srcy, int x, int y)
{
	auto Channels = 3;

	if (y >= 0 && y < srcy && x >= 0 && x < srcx)
	{
		auto index = (y * srcx + x) * Channels;

		auto r = Input[index];
		auto g = Input[index + 1];
		auto b = Input[index + 2];

		return (r << 16) + (g << 8) + b;
	}

	return 0;
}

int CLR(unsigned char* Input, int srcx, int srcy, int x, int y, int dx, int dy)
{
	auto xx = x + dx;
	auto yy = y + dy;

	if (xx < 0)
	{
		xx = 0;
	}

	if (xx > srcx - 1)
	{
		xx = srcx - 1;
	}

	if (yy < 0)
	{
		yy = 0;
	}

	if (yy > srcy - 1)
	{
		yy = srcy - 1;
	}

	return CLR(Input, srcx, srcy, xx, yy);
}

unsigned char Red(int rgb)
{
	return (unsigned char)(rgb >> 16);
}

unsigned char Green(int rgb)
{
	return (unsigned char)((rgb & 0x00FF00) >> 8);
}

unsigned char Blue(int rgb)
{
	return (unsigned char)(rgb & 0x0000FF);
}

unsigned char Brightness(int rgb)
{
	auto dwordC = rgb & 0xFFFFFF;

	return (unsigned char)((Red(dwordC) * 3 + Green(dwordC) * 3 + Blue(dwordC) * 2) >> 3);
}

unsigned char Luminance(int rgb)
{
	auto r = (double)Red(rgb);
	auto g = (double)Green(rgb);
	auto b = (double)Blue(rgb);

	return (unsigned char)(0.299 * r + 0.587 * g + 0.114 * b);
}

unsigned char ChromaU(int rgb)
{
	auto r = (double)Red(rgb);
	auto g = (double)Green(rgb);
	auto b = (double)Blue(rgb);

	return (unsigned char)(0.5 * r - 0.418688 * g - 0.081312 * b + 127.5);
}

unsigned char ChromaV(int rgb)
{
	auto r = (double)Red(rgb);
	auto g = (double)Green(rgb);
	auto b = (double)Blue(rgb);

	return (unsigned char)(-0.168736 * r - 0.331264 * g + 0.5 * b + 127.5);
}
