using System;
using System.Collections.Generic;

namespace Race;

// Rung 2 - stop the outer loop at the square root. Includes rung 1.
//
// Every composite has a prime factor no larger than its own square root, so once
// the factor passes sqrt(n), every composite below n has already been marked by
// something smaller.
//
// This removes almost no marking. What it removes is the outer loop's scan of all
// 1,000,000 entries looking for the next prime - a full megabyte streamed through
// cache to discover nothing. The saving is a read pass, not writes.
//
// limit is hoisted out rather than written as `factor * factor <= _sieveSize`,
// which is fine at a million but overflows int above about 46,341.
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
