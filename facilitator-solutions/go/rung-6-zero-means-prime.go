package main

import "math"

// Rung 6 - stop refilling the slice. Includes rungs 2 to 5.
//
// make([]uint64, n) is already zeroed, and the starting point then loops over the
// whole thing writing true. Invert the sense so that ZERO means prime and that
// entire pass disappears, every lap. This is Go's most obviously wasteful rung -
// the same trick works everywhere, but make() zeroing memory and then being
// immediately overwritten is right there in the starting point.
//
// THE TAIL WORD BUG. The slice holds ceil(count / 64) words, so the last word has
// bits for numbers past n. Those bits are zero, and zero now means prime. At
// n = 1,000,000 that is 32 spare bits covering 1,000,001 to 1,000,063, of which
// 1,000,003 is prime - so the sieve reports 78,499 primes and fails validation.
//
// Bound result by count, never by len(words)*64. This only bites once rungs 5 and
// 6 are both present.

type primeSieve struct {
	words []uint64
	size  int
}

func getSieve(sieveSize int) primeSieve {
	count := (sieveSize + 1) / 2
	return primeSieve{words: make([]uint64, (count+63)/64), size: sieveSize}
}

func (sieve primeSieve) run() {
	limit := int(math.Sqrt(float64(sieve.size)))
	count := (sieve.size + 1) / 2

	for factor := 3; factor <= limit; factor += 2 {
		f := factor >> 1

		if sieve.words[f>>6]&(1<<(uint(f)&63)) == 0 {
			for q := (factor * factor) >> 1; q < count; q += factor {
				sieve.words[q>>6] |= 1 << (uint(q) & 63)
			}
		}
	}
}

func (sieve primeSieve) result() []int {
	var primes []int
	count := (sieve.size + 1) / 2

	if sieve.size >= 2 {
		primes = append(primes, 2)
	}

	// Bounded by count, not by len(words)*64 - see the tail word note above.
	for i := 1; i < count; i++ {
		if sieve.words[i>>6]&(1<<(uint(i)&63)) == 0 {
			primes = append(primes, 2*i+1)
		}
	}

	return primes
}
