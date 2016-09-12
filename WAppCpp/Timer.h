#pragma once


namespace WAppCpp {
	class Timer
	{

	private:

		__int64 performanceFrequency;

		__int64 performanceCountStart;

	public:

		Timer();

		void start();

		long elapsedTimeMillis();

	};
}


