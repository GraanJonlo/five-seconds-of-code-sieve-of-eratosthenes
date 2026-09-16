// This is the only file you may edit.
//
// The rules:
//   * It has to stay a sieve of Eratosthenes.
//   * getSieve and run are inside the timed loop, results is not. run must leave
//     the sieve fully computed - moving sieving work into results is scoring laps
//     you never ran.
//   * No 3rd party libraries, all code must be your own.

export const getSieve = (sieveSize) => {
	const sieve = new Array(sieveSize + 1);

	for (let i = 0; i <= sieveSize; i++) {
		sieve[i] = true;
	}

	return {
		run: () => {
			for (let factor = 2; factor <= sieveSize; factor++) {
				if (sieve[factor]) {
					let q = factor + factor;
					while (q <= sieveSize) {
						sieve[q] = false;
						q += factor;
					}
				}
			}
		},
		results: () => {
			let current = 2;
			let primes = [];
			sieve.slice(2).forEach((isPrime) => {
				if (isPrime) {
					primes.push(current);
				}
				current++;
			});

			return primes;
		},
	};
};
