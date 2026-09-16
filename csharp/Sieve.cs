using System.Collections.Generic;
using System.Linq;

namespace Race;

// This is the only file you may edit.
//
// The rules:
//   * It has to stay a sieve of Eratosthenes.
//   * The constructor and Run() are inside the timed loop, Result() is not.
//     Run() must leave the sieve fully computed - moving sieving work into
//     Result() is scoring laps you never ran.
//   * No 3rd party libraries, all code must be your own.
public class Sieve
{
	private readonly int _sieveSize;
	private readonly List<bool> _sieve;

	public Sieve(int sieveSize)
	{
		_sieveSize = sieveSize;
		_sieve = [];
		for (int i = 0; i < sieveSize + 1; i++)
		{
			_sieve.Add(true);
		}
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
		int current = 2;
		List<int> primes = [];

		foreach (var number in _sieve.Skip(2))
		{
			if (number)
			{
				primes.Add(current);
			}

			current++;
		}

		return primes;
	}
}
