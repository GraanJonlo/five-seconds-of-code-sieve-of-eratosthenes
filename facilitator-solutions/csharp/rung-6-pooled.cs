using System;
using System.Collections.Generic;

namespace Race;

// Rung 6 plus a pooled buffer. OFF LADDER - this is the rules call, not a rung.
//
// The constructor is inside the timed loop, so a buffer kept across laps and merely
// cleared is a large win. Whether teams may do this is a decision the facilitator
// has to make out loud before somebody asks. The sieve is still genuinely recomputed
// every lap; only the allocation is reused.
//
// Measured here so that the rules call can be made with a number attached.
public class Sieve
{
	private const int WordBits = 64;

	private static ulong[]? _buffer;

	private readonly int _sieveSize;
	private readonly int _count;
	private readonly ulong[] _words;

	public Sieve(int sieveSize)
	{
		_sieveSize = sieveSize;
		_count = (sieveSize + 1) / 2;

		int wordCount = (_count + WordBits - 1) / WordBits;

		if (_buffer is null || _buffer.Length < wordCount)
		{
			_buffer = new ulong[wordCount];
		}
		else
		{
			Array.Clear(_buffer, 0, wordCount);
		}

		_words = _buffer;
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
