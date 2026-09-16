using System;
using System.Collections.Generic;

namespace Race;

// Rung 5 - bit packing. Includes rungs 1 to 4.
//
// One bit per odd candidate, packed into 64-bit words. Eight times smaller again,
// sixteen times smaller than the original byte-per-number array.
//
//     word index    i >> 6
//     bit within    i & 63
//     test          (words[i >> 6] & (1UL << i)) != 0
//     clear         words[i >> 6] &= ~(1UL << i)
//
// C# masks the shift count for you, so `1UL << i` behaves as `1UL << (i & 63)`.
// Go does not do this, which is why the Go version has to write the mask out.
//
// This is NOT faster per operation - a bit write is a read, a shift, an or and a
// write where a byte write is a single store. It wins because it does half the
// operations in a sixteenth of the space.
//
// Here a set bit still means prime, so the words are filled with ones. The tail
// word holds bits beyond _count; they are filled too, and Result is bounded by
// _count so they are never read. Rung 6 inverts this, and that is where the tail
// word turns into a bug.
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
		Array.Fill(_words, ulong.MaxValue);
	}

	public void Run()
	{
		int limit = (int)Math.Sqrt(_sieveSize);

		for (int factor = 3; factor <= limit; factor += 2)
		{
			int f = factor >> 1;

			if ((_words[f >> 6] & (1UL << f)) != 0)
			{
				for (int q = (factor * factor) >> 1; q < _count; q += factor)
				{
					_words[q >> 6] &= ~(1UL << q);
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
			if ((_words[i >> 6] & (1UL << i)) != 0)
			{
				primes.Add(2 * i + 1);
			}
		}

		return primes;
	}
}
