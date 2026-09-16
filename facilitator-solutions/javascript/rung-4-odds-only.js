// Rung 4 - odds only. Includes rungs 1 to 3.
//
// Index i now represents the number 2i + 1, so the array holds only the odd
// candidates: half the operations and half the footprint.
//
//   number -> index      i = x >> 1        (for odd x)
//   index  -> number     x = 2*i + 1
//
// Two traps. Index 0 represents 1, which is not prime - never a factor, never in
// the results. And 2 has to be pushed by hand, because it is the one prime an odds
// only array cannot represent.

export const getSieve = (sieveSize) => {
	const count = (sieveSize + 1) >> 1;
	const sieve = new Uint8Array(count);
	sieve.fill(1);

	const limit = Math.floor(Math.sqrt(sieveSize));

	return {
		run: () => {
			for (let factor = 3; factor <= limit; factor += 2) {
				if (sieve[factor >> 1]) {
					// Even multiples are already gone, so the stride is 2*factor in
					// number space, which is exactly factor in index space.
					for (let q = (factor * factor) >> 1; q < count; q += factor) {
						sieve[q] = 0;
					}
				}
			}
		},
		results: () => {
			const primes = [];
			if (sieveSize >= 2) {
				primes.push(2);
			}
			for (let i = 1; i < count; i++) {
				if (sieve[i]) {
					primes.push(2 * i + 1);
				}
			}
			return primes;
		},
	};
};
