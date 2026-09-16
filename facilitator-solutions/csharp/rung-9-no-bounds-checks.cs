using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Race;

// Rung 9 - bounds check removal. Rung 6 plus this, NOT a cumulative ladder step.
//
// The JIT eliminates bounds checks for recognisable `for (i = 0; i < arr.Length; i++)`
// shapes. The sieve's `q += factor` stride is not one of those, so every write pays
// for a check it does not need.
//
// POINTERS ARE NOT AVAILABLE HERE. Race.csproj sets no <AllowUnsafeBlocks>, and the
// csproj is off limits, so `fixed` and ulong* will not compile. Unsafe.Add takes a
// ref rather than a pointer, so it works without that switch. Span<T> indexing still
// bounds checks and is not the rung.
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

		if (_words.Length == 0)
		{
			return;
		}

		ref ulong words = ref MemoryMarshal.GetArrayDataReference(_words);

		for (int factor = 3; factor <= limit; factor += 2)
		{
			int f = factor >> 1;

			if ((Unsafe.Add(ref words, f >> 6) & (1UL << f)) == 0)
			{
				for (int q = (factor * factor) >> 1; q < _count; q += factor)
				{
					Unsafe.Add(ref words, q >> 6) |= 1UL << q;
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
			if ((_words[i >> 6] & (1UL << i)) == 0)
			{
				primes.Add(2 * i + 1);
			}
		}

		return primes;
	}
}
