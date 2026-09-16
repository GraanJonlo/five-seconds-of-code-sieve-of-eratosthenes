package main

import "math"

// Rung 2 - stop the outer loop at the square root.
//
// Go starts on a flat []bool already, so there is no rung 1 here - the growable
// list rung is a C# and JavaScript problem.
//
// Every composite has a prime factor no larger than its own square root, so once
// the factor passes sqrt(n) every composite below n has already been marked by
// something smaller. What this removes is the outer loop's scan of all 1,000,000
// entries looking for the next prime. The saving is a read pass, not writes.

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
			q := factor + factor
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
