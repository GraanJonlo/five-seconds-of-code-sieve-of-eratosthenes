#!/usr/bin/env node

import fs from "node:fs";

import { getSieve } from "./primeSieve.js";

// Everything in this file is off limits. primeSieve.js is the only file you may edit.

// Historical data for validating our results - the number of primes to be found
// under some limit, and the sum of those primes, such as 168 primes under 1,000
// which sum to 76,127. The sum is checked as well as the count so that returning
// the right number of wrong answers does not pass.
const validationData = {
	10: { count: 4, sum: 17 },
	100: { count: 25, sum: 1_060 },
	1_000: { count: 168, sum: 76_127 },
	10_000: { count: 1_229, sum: 5_736_396 },
	100_000: { count: 9_592, sum: 454_396_537 },
	1_000_000: { count: 78_498, sum: 37_550_402_023 },
	10_000_000: { count: 664_579, sum: 3_203_324_994_356 },
	100_000_000: { count: 5_761_455, sum: 279_209_790_387_276 },
};

const sieveSize = 1_000_000;
const baselineFile = "baseline.txt";

const sum = (primes) => primes.reduce((total, prime) => total + prime, 0);

const readBaseline = () => {
	try {
		const baseline = Number.parseInt(fs.readFileSync(baselineFile, "utf8").trim(), 10);
		return Number.isInteger(baseline) && baseline > 0 ? baseline : null;
	} catch {
		return null;
	}
};

// Self test before the race. Sieving small ranges first means an off-by-one in a
// rewritten sieve gets reported against the size that broke it, instead of showing
// up as a bare "Valid: false" five seconds later.
const selfTest = () => {
	for (const size of [10, 100, 1_000, 10_000, 100_000]) {
		const candidate = getSieve(size);
		candidate.run();

		const candidatePrimes = candidate.results();
		const candidateSum = sum(candidatePrimes);
		const expected = validationData[size];

		if (candidatePrimes.length !== expected.count || candidateSum !== expected.sum) {
			return `Self test FAILED at sieve size ${size}: expected ${expected.count} primes summing to ${expected.sum}, got ${candidatePrimes.length} summing to ${candidateSum}`;
		}
	}

	return null;
};

const race = () => {
	const failure = selfTest();

	if (failure !== null) {
		console.log(failure);
		return 1;
	}

	console.log("Self test passed");

	let laps = 0;
	let sieve = getSieve(sieveSize);

	const startTime = performance.now();

	while (performance.now() - startTime < 5000) {
		sieve = getSieve(sieveSize);
		sieve.run();
		laps++;
	}

	const elapsed = performance.now() - startTime;

	const primes = sieve.results();
	const primeSum = sum(primes);
	const expected = validationData[sieveSize];
	const valid = primes.length === expected.count && primeSum === expected.sum;

	console.log(
		`Laps: ${laps} Time: ${elapsed / 1000} #Primes: ${primes.length} Valid: ${valid}`
	);

	if (!valid) {
		return 1;
	}

	// Record the first valid result as this machine's baseline, then report every later
	// run as a multiple of it. Run once before changing anything to get an honest one.
	const baseline = readBaseline();

	if (baseline !== null) {
		console.log(`Speedup: ${(laps / baseline).toFixed(2)}x baseline (${baseline} laps)`);
	} else {
		fs.writeFileSync(baselineFile, String(laps));
		console.log(
			`Baseline recorded: ${laps} laps. Now optimise primeSieve.js. Delete ${baselineFile} to re-record.`
		);
	}

	return 0;
};

process.exitCode = race();
