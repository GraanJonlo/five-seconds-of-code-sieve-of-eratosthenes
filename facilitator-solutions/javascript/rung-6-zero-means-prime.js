// Rung 6 - stop refilling the array. Includes rungs 1 to 5.
//
// A new Uint32Array is already zeroed. Invert the sense so that ZERO means prime
// and the fill disappears entirely - a whole pass over the array, deleted, every
// lap. The runtime was already zeroing it; we stopped fighting that.
//
// THE TAIL WORD BUG. The array holds ceil(count / 32) words, so the last word has
// bits for numbers past n. Those bits are zero, and zero now means prime. At
// n = 1,000,000 that is 16 spare bits covering 1,000,001 to 1,000,031, of which
// 1,000,003 is prime - so the sieve reports 78,499 primes and fails validation.
//
// Bound results by count, never by words.length * 32. This only bites once rungs 5
// and 6 are both present: rung 5 alone fills with ones, and a Uint8Array has no
// padding to get wrong.

export const getSieve = (sieveSize) => {
	const count = (sieveSize + 1) >> 1;
	const words = new Uint32Array((count + 31) >>> 5);

	const limit = Math.floor(Math.sqrt(sieveSize));

	return {
		run: () => {
			for (let factor = 3; factor <= limit; factor += 2) {
				const f = factor >> 1;

				if ((words[f >>> 5] & (1 << f)) === 0) {
					for (let q = (factor * factor) >> 1; q < count; q += factor) {
						words[q >>> 5] |= 1 << q;
					}
				}
			}
		},
		results: () => {
			const primes = [];
			if (sieveSize >= 2) {
				primes.push(2);
			}
			// Bounded by count, not words.length * 32 - see the tail word note.
			for (let i = 1; i < count; i++) {
				if ((words[i >>> 5] & (1 << i)) === 0) {
					primes.push(2 * i + 1);
				}
			}
			return primes;
		},
	};
};
