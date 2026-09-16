using System;
using System.Collections.Generic;

namespace Race;

// Rung 7 - cache blocking. Rung 6 plus blocking, NOT a cumulative ladder step.
//
// Instead of striding each prime across the whole array in turn, walk the array in
// blocks sized to fit L1 and mark the multiples of every prime within a block
// before moving on. Each prime remembers where it got to.
//
// Expect this to do very little at n = 1,000,000. An odds only bitset is already
// only 61 KiB and sits comfortably in L2, so there is not much left to win.
// Blocking is the right answer at a hundred million, not at a million.
public class Sieve
{
	private const int WordBits = 64;
	private const int BlockBits = 32 * 1024 * 8;   // 32 KiB of bitset per block

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

		if (limit < 3)
		{
			return;
		}

		// A small plain sieve to enumerate the sieving primes, which is cheap:
		// at n = 1,000,000 this is 500 bytes of work.
		int smallCount = (limit + 1) / 2;
		bool[] smallComposite = new bool[smallCount];

		for (int factor = 3; factor * factor <= limit; factor += 2)
		{
			if (!smallComposite[factor >> 1])
			{
				for (int q = (factor * factor) >> 1; q < smallCount; q += factor)
				{
					smallComposite[q] = true;
				}
			}
		}

		int[] primes = new int[smallCount];
		int[] next = new int[smallCount];
		int primeCount = 0;

		for (int i = 1; i < smallCount; i++)
		{
			if (!smallComposite[i])
			{
				int p = 2 * i + 1;
				primes[primeCount] = p;
				next[primeCount] = (p * p) >> 1;
				primeCount++;
			}
		}

		// Now sweep the sieve one L1 sized block at a time, applying every prime to
		// the block before moving on. next[] carries each prime's position across.
		for (int lo = 0; lo < _count; lo += BlockBits)
		{
			int hi = Math.Min(lo + BlockBits, _count);

			for (int k = 0; k < primeCount; k++)
			{
				int p = primes[k];
				int q = next[k];

				for (; q < hi; q += p)
				{
					_words[q >> 6] |= 1UL << q;
				}

				next[k] = q;
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
