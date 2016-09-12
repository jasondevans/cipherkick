#include "stdafx.h"

#include "Timer.h"


using namespace WAppCpp;


Timer::Timer()
{
	start();
}


void Timer::start()
{
	QueryPerformanceFrequency((LARGE_INTEGER*) &performanceFrequency);
	QueryPerformanceCounter((LARGE_INTEGER*) &performanceCountStart);
}


long Timer::elapsedTimeMillis()
{
	__int64 performanceCountCurrent;
	QueryPerformanceCounter((LARGE_INTEGER*) &performanceCountCurrent);
	__int64 elapsedCounts = performanceCountCurrent - performanceCountStart;
	
	return (long) floor(((double) elapsedCounts) / ((double) performanceFrequency / 1000) + 0.5);
}