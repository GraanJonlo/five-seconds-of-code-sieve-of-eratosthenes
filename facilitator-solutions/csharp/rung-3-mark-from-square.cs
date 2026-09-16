using System;
using System.Collections.Generic;

namespace Race;

// Rung 3 - start marking at f*f, not 2f. Includes rungs 1 and 2.
//
// Any multiple k*f where k < f has a factor smaller than f, so it was already
// marked when that smaller factor had its turn. For the factor 7, everything from
// 14 through 42 was covered by 2, 3 and 5. The first multiple of 7 that nothing
// else has reached is 49.
//
// factor * factor cannot overflow here because factor <= sqrt(_sieveSize).
public class Sieve
{
	private readonly int _sieveSize;
	private readonly bool[] _sieve;

	public Sieve(int sieveSize)
	{
		_sieveSize = sieveSize;
		_sieve = new bool[sieveSize + 1];
		Array.Fill(_sieve, true);
	}

	public void Run()
	{
		int limit = (int)Math.Sqrt(_sieveSize);

		for (int factor = 2; factor <= limit; factor++)
		{
			if (_sieve[factor])
			{
				int q = factor * factor;
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
		List<int> primes = [];

		for (int i = 2; i <= _sieveSize; i++)
		{
			if (_sieve[i])
			{
				primes.Add(i);
			}
		}

		return primes;
	}
}
