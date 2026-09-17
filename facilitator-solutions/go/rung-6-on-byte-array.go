package main

import "math"

// Rung 6 applied to rung 4, WITHOUT the bitset. Rungs 2, 3, 4 and 6.
//
// make([]bool, n) is already zeroed, and the starting point then loops over the
// whole thing writing true. Invert the sense so false means prime and that entire
// pass disappears - without needing the bitset underneath it.
//
// Measured because the nudge order depends on it: rung 5 can measure negative,
// this cannot, because it only removes work.
//
// No tail word problem. The slice is exactly count long.

type primeSieve struct {
	flags []bool
	size  int
}

func getSieve(sieveSize int) primeSieve {
	// make() already zeroes this, and false now means prime - so no fill at all.
	count := (sieveSize + 1) / 2
	return primeSieve{flags: make([]bool, count), size: sieveSize}
}

func (sieve primeSieve) run() {
	limit := int(math.Sqrt(float64(sieve.size)))
	count := len(sieve.flags)

	for factor := 3; factor <= limit; factor += 2 {
		if !sieve.flags[factor>>1] {
			for q := (factor * factor) >> 1; q < count; q += factor {
				sieve.flags[q] = true
			}
		}
	}
}

func (sieve primeSieve) result() []int {
	var primes []int

	if sieve.size >= 2 {
		primes = append(primes, 2)
	}

	for i := 1; i < len(sieve.flags); i++ {
		if !sieve.flags[i] {
			primes = append(primes, 2*i+1)
		}
	}

	return primes
}
