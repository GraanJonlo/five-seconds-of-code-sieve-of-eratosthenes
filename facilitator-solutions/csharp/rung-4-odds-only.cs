using System;
using System.Collections.Generic;

namespace Race;

// Rung 4 - odds only. Includes rungs 1 to 3.
//
// Every even number above 2 is composite, so half the array was never going to be
// prime. Store only the odd numbers: index i represents the number 2i + 1.
//
//     number -> index      i = x >> 1        (for odd x)
//     index  -> number     x = 2 * i + 1
//
// This halves the operations AND the footprint. It is the first rung with an index
// mapping, so the first one that breaks.
//
// Two traps. Index 0 represents 1, which is not prime - it is never a factor and
// never appears in the results. And 2 has to be prepended by hand, because it is
// the one prime the array cannot represent.
public class Sieve
{
	private readonly int _sieveSize;
	private readonly int _count;
	private readonly bool[] _sieve;

	public Sieve(int sieveSize)
	{
		_sieveSize = sieveSize;
		_count = (sieveSize + 1) / 2;
		_sieve = new bool[_count];
		Array.Fill(_sieve, true);
	}

	public void Run()
	{
		int limit = (int)Math.Sqrt(_sieveSize);

		for (int factor = 3; factor <= limit; factor += 2)
		{
			if (_sieve[factor >> 1])
			{
				// The even multiples are already gone, so the stride is 2*factor in
				// number space, which is exactly factor in index space.
				for (int q = (factor * factor) >> 1; q < _count; q += factor)
				{
					_sieve[q] = false;
				}
			}
		}
	}

	public List<int> Result()
	{
		List<int> primes = [];

		if (_sieveSize >= 2)
		{
			primes.Add(2);
		}

		for (int i = 1; i < _count; i++)
		{
			if (_sieve[i])
			{
				primes.Add(2 * i + 1);
			}
		}

		return primes;
	}
}
