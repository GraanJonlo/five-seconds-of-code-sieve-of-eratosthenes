package main

import "math"

// Rung 5 - bit packing. Includes rungs 2 to 4.
//
// One bit per odd candidate, packed into 64-bit words. Eight times smaller again,
// sixteen times smaller than the original byte-per-number slice.
//
//	word index    i >> 6
//	bit within    i & 63
//
// GO DOES NOT MASK SHIFT COUNTS. In C# and JavaScript, 1 << i quietly behaves as
// 1 << (i & wordBits-1). In Go a shift count at or beyond the word width produces
// zero, silently, so the mask has to be written out: 1 << (uint(q) & 63). A Go
// bitset that "nearly works" is usually this. Do not tidy it away.
//
// A set bit still means prime here, so the words are filled with ones. The tail
// word's spare bits are set too, and result is bounded by count so they are never
// read - rung 6 is where that turns into a bug.

type primeSieve struct {
	words []uint64
	size  int
}

func getSieve(sieveSize int) primeSieve {
	count := (sieveSize + 1) / 2
	words := make([]uint64, (count+63)/64)
	for i := range words {
		words[i] = ^uint64(0)
	}
	return primeSieve{words: words, size: sieveSize}
}

func (sieve primeSieve) run() {
	limit := int(math.Sqrt(float64(sieve.size)))
	count := (sieve.size + 1) / 2

	for factor := 3; factor <= limit; factor += 2 {
		f := factor >> 1

		if sieve.words[f>>6]&(1<<(uint(f)&63)) != 0 {
			for q := (factor * factor) >> 1; q < count; q += factor {
				sieve.words[q>>6] &^= 1 << (uint(q) & 63)
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

	for i := 1; i < count; i++ {
		if sieve.words[i>>6]&(1<<(uint(i)&63)) != 0 {
			primes = append(primes, 2*i+1)
		}
	}

	return primes
}
