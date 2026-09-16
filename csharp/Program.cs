using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Race;

// Top level timing code is off limits

Dictionary<int, int> validationData = new Dictionary<int, int>
{
	{10, 4}, // Historical data for validating our results - the number of primes
	{100, 25}, // to be found under some limit, such as 168 primes under 1000
	{1_000, 168},
	{10_000, 1229},
	{100_000, 9592},
	{1_000_000, 78498},
	{10_000_000, 664579},
	{100_000_000, 5761455}
};

const int sieveSize = 1_000_000;
int laps = 0;
Sieve? sieve = null;

var stopwatch = Stopwatch.StartNew();

while (stopwatch.Elapsed.Seconds < 5)
{
	sieve = new Sieve(sieveSize);
	sieve.Run();
	laps++;
}

stopwatch.Stop();

List<int> primes = sieve != null ? sieve.Result() : [];

bool correctNumberOfPrimesFound = primes.Count == validationData[sieveSize];

Console.WriteLine(
	$"Laps: {laps} Time: {stopwatch.Elapsed.TotalSeconds} #Primes: {primes.Count} Valid: {correctNumberOfPrimesFound}");

namespace Race
{
	// The Sieve class is where you are allowed to make changes
	public class Sieve
	{
		private readonly int _sieveSize;
		private readonly List<bool> _sieve;

		public Sieve(int sieveSize)
		{
			_sieveSize = sieveSize;
			_sieve = [];
			for (int i = 0; i < sieveSize + 1; i++)
			{
				_sieve.Add(true);
			}
		}

		public void Run()
		{
			for (int factor = 2; factor <= _sieveSize; factor++)
			{
				if (_sieve[factor])
				{
					int q = factor + factor;
					while (q <= _sieveSize)
					{
						_sieve[q] = false;
						q += factor;
					}
				}
			}
		}

		public List<int> Result()
		{
			int current = 2;
			List<int> primes = [];

			foreach (var number in _sieve.Skip(2))
			{
				if (number)
				{
					primes.Add(current);
				}

				current++;
			}

			return primes;
		}
	}
}
