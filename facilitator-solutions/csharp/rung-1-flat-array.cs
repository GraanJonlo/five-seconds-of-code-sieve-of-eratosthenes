using System;
using System.Collections.Generic;

namespace Race;

// Rung 1 - growable list to fixed array.
//
// List<bool>.Add doubles capacity when it runs out, so building a million entries
// means about twenty reallocations, each copying everything written so far. The
// indexer is a method call carrying its own bounds check rather than a direct
// array access.
//
// Note the array is zero initialised, which means false, and here true means
// prime - hence the Array.Fill. Rung 6 gets rid of that fill by inverting the
// sense instead.
//
// Sieve must stay a class: Program.cs declares `Sieve? sieve = null`, and as a
// struct that becomes Nullable<Sieve>, which has no Result().
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
