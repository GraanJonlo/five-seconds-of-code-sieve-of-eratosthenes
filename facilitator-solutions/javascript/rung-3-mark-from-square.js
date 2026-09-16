// Rung 3 - start marking at f*f, not 2f. Includes rungs 1 and 2.
//
// Any multiple k*f where k < f has a factor smaller than f, so it was already
// marked when that smaller factor had its turn. For the factor 7, everything from
// 14 through 42 was covered by 2, 3 and 5. The first multiple of 7 that nothing
// else has reached is 49.

export const getSieve = (sieveSize) => {
	const sieve = new Uint8Array(sieveSize + 1);
	sieve.fill(1);

	const limit = Math.floor(Math.sqrt(sieveSize));

	return {
		run: () => {
			for (let factor = 2; factor <= limit; factor++) {
				if (sieve[factor]) {
					let q = factor * factor;
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
