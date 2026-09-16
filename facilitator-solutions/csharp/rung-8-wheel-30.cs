using System;
using System.Collections.Generic;

namespace Race;

// Rung 8 - wheel factorization, mod 30. Rung 6 replaced, NOT a cumulative step.
//
// Odds only keeps 15 candidates in every 30. The residues coprime to 30 are
// 1, 7, 11, 13, 17, 19, 23 and 29 - eight in every thirty, so another 1.9x less
// space again. One byte covers thirty numbers, one bit per residue.
//
//     number -> (byte, bit)    x / 30, BitIndex[x % 30]
//
// The index arithmetic stops being a single shift, which is the whole cost of this
// rung. For a prime p = 30a + pr and a multiple q = 30b + qr:
//
//     p*q = 30 * (30ab + a*qr + b*pr + (pr*qr)/30) + (pr*qr) % 30
//
// so for a fixed residue of q, the byte index of successive multiples goes up by
// exactly 30a + pr = p. That gives eight arithmetic progressions per prime, each
// with a fixed bit position, which is what the inner loop walks.
//
// Realistically this is a rung to mention in the debrief rather than one anybody
// reaches in an afternoon.
public class Sieve
{
	private static readonly int[] Wheel = [1, 7, 11, 13, 17, 19, 23, 29];

	private static readonly int[] BitIndex =
	[
		-1,  0, -1, -1, -1, -1, -1,  1, -1, -1,
		-1,  2, -1,  3, -1, -1, -1,  4, -1,  5,
		-1, -1, -1,  6, -1, -1, -1, -1, -1,  7
	];

	private readonly int _sieveSize;
	private readonly byte[] _data;

	public Sieve(int sieveSize)
	{
		_sieveSize = sieveSize;
		_data = new byte[sieveSize / 30 + 1];
	}

	public void Run()
	{
		int limit = (int)Math.Sqrt(_sieveSize);
		int byteCount = _data.Length;

		for (int a = 0; 30 * a + 1 <= limit; a++)
		{
			for (int ip = 0; ip < 8; ip++)
			{
				int pr = Wheel[ip];
				int p = 30 * a + pr;

				if (p < 7)
				{
					continue;   // 1 is not prime and never a factor
				}

				if (p > limit)
				{
					break;
				}

				if ((_data[a] & (1 << ip)) != 0)
				{
					continue;   // already marked composite
				}

				for (int jq = 0; jq < 8; jq++)
				{
					int qr = Wheel[jq];

					// Smallest multiplier q >= p carrying this residue.
					int b = qr >= pr ? a : a + 1;

					int product = pr * qr;
					int byteIdx = (30 * a * b) + (a * qr) + (b * pr) + (product / 30);
					byte mask = (byte)(1 << BitIndex[product % 30]);

					while (byteIdx < byteCount)
					{
						_data[byteIdx] |= mask;
						byteIdx += p;
					}
				}
			}
		}
	}

	public List<int> Result()
	{
		List<int> primes = [];

		// The wheel cannot represent 2, 3 or 5 - they are the wheel.
		if (_sieveSize >= 2) { primes.Add(2); }
		if (_sieveSize >= 3) { primes.Add(3); }
		if (_sieveSize >= 5) { primes.Add(5); }

		for (int a = 0; a < _data.Length; a++)
		{
			int bits = _data[a];

			for (int j = 0; j < 8; j++)
			{
				int x = (30 * a) + Wheel[j];

				if (x < 7)
				{
					continue;   // the number 1
				}

				if (x > _sieveSize)
				{
					return primes;   // tail padding - bits past n read as prime
				}

				if ((bits & (1 << j)) == 0)
				{
					primes.Add(x);
				}
			}
		}

		return primes;
	}
}
