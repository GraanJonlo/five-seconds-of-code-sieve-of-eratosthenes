// Rung 6 applied to rung 4, WITHOUT the bitset. Rungs 1, 2, 3, 4 and 6.
//
// A new Uint8Array is already zeroed. Invert the sense so that zero means prime and
// the fill disappears entirely, every lap - no bitset required underneath it.
//
// This matters more in JavaScript than anywhere else, because rung 5 measures as a
// regression here: the odds only Uint8Array is JavaScript's best result on the
// ladder, and this is the cheapest thing that can be added to it.
//
// No tail word problem. The array is exactly count long, so there are no spare
// slots past n to read back as prime.

export const getSieve = (sieveSize) => {
	const count = (sieveSize + 1) >> 1;

	// Already zeroed, and 0 now means prime - so no fill at all.
	const sieve = new Uint8Array(count);

	const limit = Math.floor(Math.sqrt(sieveSize));

	return {
		run: () => {
			for (let factor = 3; factor <= limit; factor += 2) {
				if (sieve[factor >> 1] === 0) {
					for (let q = (factor * factor) >> 1; q < count; q += factor) {
						sieve[q] = 1;
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
				if (sieve[i] === 0) {
					primes.push(2 * i + 1);
				}
			}
			return primes;
		},
	};
};
