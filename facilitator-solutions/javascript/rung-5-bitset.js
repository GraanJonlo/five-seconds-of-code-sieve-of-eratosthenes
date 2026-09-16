// Rung 5 - bit packing. Includes rungs 1 to 4.
//
// One bit per odd candidate. Eight times smaller again, sixteen times smaller than
// the Uint8Array, and a sixty-fourth of where the JavaScript starting point began.
//
// JAVASCRIPT'S BITWISE OPERATORS ARE 32 BIT, so this wants a Uint32Array with
// >>> 5 and & 31, not the 64-bit word layout the .NET versions use. BigUint64Array
// drags in BigInt and is slower, not faster.
//
//   word index    i >>> 5
//   bit within    i & 31
//
// JavaScript masks the shift count for you, so `1 << i` already behaves as
// `1 << (i & 31)`. Go does not, which is why the Go version writes the mask out.
//
// A set bit still means prime here, so the words are filled with ones and the tail
// word's spare bits are set too. results is bounded by count, so they are never
// read - rung 6 is where that becomes a bug.
//
// Expect this to add less here than it does in C#. A Uint8Array is already fast
// and already small enough at a million.

export const getSieve = (sieveSize) => {
	const count = (sieveSize + 1) >> 1;
	const words = new Uint32Array((count + 31) >>> 5);
	words.fill(0xffffffff);

	const limit = Math.floor(Math.sqrt(sieveSize));

	return {
		run: () => {
			for (let factor = 3; factor <= limit; factor += 2) {
				const f = factor >> 1;

				if ((words[f >>> 5] & (1 << f)) !== 0) {
					for (let q = (factor * factor) >> 1; q < count; q += factor) {
						words[q >>> 5] &= ~(1 << q);
					}
				}
			}
		},
		results: () => {
			const primes = [];
			if (sieveSize >= 2) {
				primes.push(2);
			}
			// !== 0 and not > 0: a bit test with bit 31 set comes back negative.
			for (let i = 1; i < count; i++) {
				if ((words[i >>> 5] & (1 << i)) !== 0) {
					primes.push(2 * i + 1);
				}
			}
			return primes;
		},
	};
};
