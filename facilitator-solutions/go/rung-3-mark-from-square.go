package main

import "math"

// Rung 3 - start marking at f*f, not 2f. Includes rung 2.
//
// Any multiple k*f where k < f has a factor smaller than f, so it was already
// marked when that smaller factor had its turn. For the factor 7, everything from
// 14 through 42 was covered by 2, 3 and 5. The first multiple of 7 that nothing
// else has reached is 49.

type primeSieve []bool

func getSieve(sieveSize int) primeSieve {
	sieve := make([]bool, sieveSize+1)
	for i := range sieve {
		sieve[i] = true
	}
	return sieve
}

func (sieve primeSieve) run() {
	sieveSize := len(sieve) - 1
	limit := int(math.Sqrt(float64(sieveSize)))

	for factor := 2; factor <= limit; factor++ {
		if sieve[factor] {
			q := factor * factor
			for q <= sieveSize {
				sieve[q] = false
				q += factor
			}
		}
	}
}

func (sieve primeSieve) result() []int {
	var primes []int

	for i := 2; i < len(sieve); i++ {
		if sieve[i] {
			primes = append(primes, i)
		}
	}

	return primes
}
