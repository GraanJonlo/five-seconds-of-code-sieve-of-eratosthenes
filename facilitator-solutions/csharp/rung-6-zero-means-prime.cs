using System;
using System.Collections.Generic;

namespace Race;

// Rung 6 - stop refilling the array. Includes rungs 1 to 5.
//
// new ulong[n] hands back zeroed memory. Invert the sense so that ZERO means prime
// and the fill disappears entirely - a whole pass over the array, deleted, every
// lap. The runtime was already zeroing it; we just stopped fighting that.
//
//     test prime    (words[i >> 6] & (1UL << i)) == 0
//     mark composite words[i >> 6] |= 1UL << i
//
// THE TAIL WORD BUG. The word array covers ceil(_count / 64) words, so the last
// word holds bits for numbers beyond n. Those bits are zero, and zero now means
// prime. At n = 1,000,000 that is 32 spare bits covering 1,000,001 to 1,000,063,
// of which 1,000,003 is prime - so the sieve reports 78,499 primes and fails.
//
// The fix is to bound Result by _count and never by _words.Length * 64. Note this
// only bites once rungs 5 and 6 are BOTH present: rung 5 alone fills with ones, and
// a byte array has no padding to get wrong.
public class Sieve
{
	private const int WordBits = 64;

	private readonly int _sieveSize;
	private readonly int _count;
	private readonly ulong[] _words;

	public Sieve(int sieveSize)
	{
		_sieveSize = sieveSize;
		_count = (sieveSize + 1) / 2;
		_words = new ulong[(_count + WordBits - 1) / WordBits];
	}

	public void Run()
	{
		int limit = (int)Math.Sqrt(_sieveSize);

		for (int factor = 3; factor <= limit; factor += 2)
		{
			int f = factor >> 1;

			if ((_words[f >> 6] & (1UL << f)) == 0)
			{
				for (int q = (factor * factor) >> 1; q < _count; q += factor)
				{
					_words[q >> 6] |= 1UL << q;
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

		// Bounded by _count, not by _words.Length * 64 - see the tail word note above.
		for (int i = 1; i < _count; i++)
		{
			if ((_words[i >> 6] & (1UL << i)) == 0)
			{
				primes.Add(2 * i + 1);
			}
		}

		return primes;
	}
}
