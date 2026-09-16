// Rung 2 - stop the outer loop at the square root. Includes rung 1.
//
// Every composite has a prime factor no larger than its own square root, so once
// the factor passes sqrt(n) every composite below n has already been marked by
// something smaller.
//
// What this removes is the outer loop's scan of all 1,000,000 entries looking for
// the next prime. The saving is a read pass, not writes.

export const getSieve = (sieveSize) => {
	const sieve = new Uint8Array(sieveSize + 1);
	sieve.fill(1);

	const limit = Math.floor(Math.sqrt(sieveSize));

	return {
		run: () => {
			for (let factor = 2; factor <= limit; factor++) {
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
