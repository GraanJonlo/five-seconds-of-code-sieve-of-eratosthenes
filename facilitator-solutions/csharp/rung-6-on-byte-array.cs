using System;
using System.Collections.Generic;

namespace Race;

// Rung 6 applied to rung 4, WITHOUT the bitset. Rungs 1, 2, 3, 4 and 6.
//
// Measured because the nudge order depends on it. Rung 6 is usually described as
// sitting on top of rung 5, but it does not need to: inverting the sense so that
// zero means prime works just as well on the odds only byte array, and deletes the
// Array.Fill of half a megabyte every lap.
//
// This matters for what you tell a pair who has just reached rung 4 and has time
// left. Rung 5 can measure negative; this cannot, because it only ever removes
// work - there is no extra instruction per write to pay for.
//
// Note there is no tail word problem here. The array is exactly _count long, so
// there are no spare slots past n to read back as prime. That trap only exists
// once the candidates are packed into words, which is a good reason to reach for
// this before reaching for the bitset.
public class Sieve
{
	private readonly int _sieveSize;
	private readonly int _count;
	private readonly bool[] _sieve;

	public Sieve(int sieveSize)
	{
		_sieveSize = sieveSize;
		_count = (sieveSize + 1) / 2;

		// Zero initialised, and false now means prime - so no fill at all.
		_sieve = new bool[_count];
	}

	public void Run()
	{
		int limit = (int)Math.Sqrt(_sieveSize);

		for (int factor = 3; factor <= limit; factor += 2)
		{
			if (!_sieve[factor >> 1])
			{
				for (int q = (factor * factor) >> 1; q < _count; q += factor)
				{
					_sieve[q] = true;
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
			if (!_sieve[i])
			{
				primes.Add(2 * i + 1);
			}
		}

		return primes;
	}
}
