// Rung 1 - Array to Uint8Array. The biggest single rung in the exercise.
//
// `new Array(n)` filled in a loop is HOLEY_ELEMENTS holding tagged values: eight
// bytes per candidate, 7.64 MiB for a million-entry sieve. A Uint8Array is one
// byte per candidate, 977 KiB. That is eight times the memory of the Go and C#
// starting points for exactly the same sieve, and it is most of why JavaScript
// posts the slowest baseline of the four.
//
// It is not only a cache story either - a tagged array is slower even at sizes
// that fit in L1, because every access pays a hole check and a type check.

export const getSieve = (sieveSize) => {
	const sieve = new Uint8Array(sieveSize + 1);
	sieve.fill(1);

	return {
		run: () => {
			for (let factor = 2; factor <= sieveSize; factor++) {
				if (sieve[factor]) {
					let q = factor + factor;
					while (q <= sieveSize) {
						sieve[q] = 0;
						q += factor;
					}
				}
			}
		},
		results: () => {
			const primes = [];
			for (let i = 2; i <= sieveSize; i++) {
				if (sieve[i]) {
					primes.push(i);
				}
			}
			return primes;
		},
	};
};
