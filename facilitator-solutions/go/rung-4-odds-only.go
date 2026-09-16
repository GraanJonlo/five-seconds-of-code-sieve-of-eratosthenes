package main

import "math"

// Rung 4 - odds only. Includes rungs 2 and 3.
//
// Index i now represents the number 2i + 1, so the slice holds only the odd
// candidates: half the operations and half the footprint.
//
//	number -> index      i = x >> 1        (for odd x)
//	index  -> number     x = 2*i + 1
//
// The slice on its own can no longer tell you n, so the sieve carries its size.
// race.go never names this type - it uses := on getSieve - so turning it into a
// struct is free. A value receiver still mutates the sieve, because the slice
// header is copied but the backing array is shared.

type primeSieve struct {
	flags []bool
	size  int
}

func getSieve(sieveSize int) primeSieve {
	count := (sieveSize + 1) / 2
	flags := make([]bool, count)
	for i := range flags {
		flags[i] = true
	}
	return primeSieve{flags: flags, size: sieveSize}
}

func (sieve primeSieve) run() {
	limit := int(math.Sqrt(float64(sieve.size)))
	count := len(sieve.flags)

	for factor := 3; factor <= limit; factor += 2 {
		if sieve.flags[factor>>1] {
			// Even multiples are already gone, so the stride is 2*factor in
			// number space, which is exactly factor in index space.
			for q := (factor * factor) >> 1; q < count; q += factor {
				sieve.flags[q] = false
			}
		}
	}
}

func (sieve primeSieve) result() []int {
	var primes []int

	// 2 is the one prime an odds only slice cannot represent.
	if sieve.size >= 2 {
		primes = append(primes, 2)
	}

	// Index 0 is the number 1, which is not prime.
	for i := 1; i < len(sieve.flags); i++ {
		if sieve.flags[i] {
			primes = append(primes, 2*i+1)
		}
	}

	return primes
}
