// The JavaScript twin of the C# cache-cliff app. Run it with:
//
//     node cache-cliff.mjs
//
// Add --expose-gc to also measure how much memory each representation really
// takes, which in JavaScript is the more surprising half of the story:
//
//     node --expose-gc cache-cliff.mjs

import fs from "node:fs";

const RACE = 1_000_000;

// ---------------------------------------------------------------- the sieves

// What primeSieve.js does today: a plain Array holding booleans.
function sieveArray(sieve, n) {
	const limit = Math.floor(Math.sqrt(n));
	for (let f = 2; f <= limit; f++) {
		if (sieve[f]) {
			for (let q = f * f; q <= n; q += f) sieve[q] = false;
		}
	}
}

function countArray(sieve, n) {
	let ops = 0;
	const limit = Math.floor(Math.sqrt(n));
	for (let f = 2; f <= limit; f++) {
		if (sieve[f]) {
			for (let q = f * f; q <= n; q += f) {
				sieve[q] = false;
				ops++;
			}
		}
	}
	return ops;
}

// One byte per candidate, in a typed array.
function sieveBytes(sieve, n) {
	const limit = Math.floor(Math.sqrt(n));
	for (let f = 2; f <= limit; f++) {
		if (sieve[f] === 1) {
			for (let q = f * f; q <= n; q += f) sieve[q] = 0;
		}
	}
}

function countBytes(sieve, n) {
	let ops = 0;
	const limit = Math.floor(Math.sqrt(n));
	for (let f = 2; f <= limit; f++) {
		if (sieve[f] === 1) {
			for (let q = f * f; q <= n; q += f) {
				sieve[q] = 0;
				ops++;
			}
		}
	}
	return ops;
}

// One bit per odd candidate. Bit i represents the number 2i+1.
// JavaScript's bitwise operators are 32 bit, so the words are 32 bit too.
function sieveBits(words, n) {
	const bits = (n + 1) >>> 1;
	const limit = Math.floor(Math.sqrt(n));
	for (let f = 3; f <= limit; f += 2) {
		const fi = f >>> 1;
		if ((words[fi >>> 5] & (1 << (fi & 31))) !== 0) continue;
		for (let q = (f * f) >>> 1; q < bits; q += f) words[q >>> 5] |= 1 << (q & 31);
	}
}

function countBits(words, n) {
	let ops = 0;
	const bits = (n + 1) >>> 1;
	const limit = Math.floor(Math.sqrt(n));
	for (let f = 3; f <= limit; f += 2) {
		const fi = f >>> 1;
		if ((words[fi >>> 5] & (1 << (fi & 31))) !== 0) continue;
		for (let q = (f * f) >>> 1; q < bits; q += f) {
			words[q >>> 5] |= 1 << (q & 31);
			ops++;
		}
	}
	return ops;
}

// ---------------------------------------------------------------- allocation

const makeArray = (n) => {
	const a = new Array(n + 1);
	for (let i = 0; i <= n; i++) a[i] = true;
	return a;
};
const makeBytes = (n) => new Uint8Array(n + 1).fill(1);
const makeWords = (n) => new Uint32Array(((((n + 1) >>> 1) + 31) >>> 5) + 1);

// ------------------------------------------------------------------- helpers

function formatBytes(bytes) {
	const abs = Math.abs(bytes);
	if (abs >= 1048576) return `${round(bytes / 1048576)} MiB`;
	if (abs >= 1024) return `${round(bytes / 1024)} KiB`;
	return `${bytes} B`;
}

function round(value) {
	return Math.abs(value - Math.round(value)) < 0.05 ? value.toFixed(0) : value.toFixed(1);
}

function measure(reset, run) {
	// Tiered JIT needs several calls before it promotes a function. Without this
	// the small sizes read far too slow and flatten the effect we are showing.
	const warmStart = performance.now();
	let warmReps = 0;
	while (performance.now() - warmStart < 100 && warmReps < 30) {
		reset();
		run();
		warmReps++;
	}

	let total = 0;
	let reps = 0;
	while (total < 150 && reps < 2000) {
		reset();
		const start = performance.now();
		run();
		total += performance.now() - start;
		reps++;
	}
	return total / reps;
}

// -------------------------------------------------------------------- memory

console.log("Cache cliff - what one sieve operation costs as the working set grows");
console.log();

if (typeof global.gc === "function") {
	console.log(`Memory actually used for a sieve of n = ${RACE.toLocaleString()}:`);
	console.log();

	const keepAlive = [];

	const footprint = (label, make) => {
		global.gc();
		global.gc();
		const before = process.memoryUsage();
		keepAlive.push(make());
		global.gc();
		const after = process.memoryUsage();
		const total =
			after.heapUsed - before.heapUsed + (after.arrayBuffers - before.arrayBuffers);
		console.log(
			`  ${label.padEnd(32)} ${formatBytes(total).padStart(9)}   ${(
				total /
				(RACE + 1)
			).toFixed(2)} bytes per candidate`
		);
	};

	footprint("new Array(n), as primeSieve.js", () => makeArray(RACE));
	footprint("Uint8Array(n)", () => makeBytes(RACE));
	footprint("Uint32Array bitset, odds only", () => makeWords(RACE));

	console.log();
	console.log("  A plain Array holds tagged values, not bytes. That is the whole reason");
	console.log("  the JavaScript race starts slower than the Go one on identical logic.");
	console.log(`  (holding ${keepAlive.length} arrays so the collector cannot cheat)`);
} else {
	console.log("Re-run with --expose-gc to also measure memory per representation,");
	console.log("which in JavaScript is the more surprising half of the story:");
	console.log();
	console.log("    node --expose-gc cache-cliff.mjs");
}

// --------------------------------------------------------------- the measure

const sizes = [];
for (let k = 0; k <= 22; k++) sizes.push(Math.round(Math.pow(2, 13 + k / 2)));
sizes.push(RACE);
const allSizes = [...new Set(sizes)].sort((a, b) => a - b);

console.log();
console.log(`Measuring ${allSizes.length} sizes. This takes a minute or two.`);
console.log();

// Warm everything once at a middling size, so the first timed size is not the
// one paying for the JIT.
{
	const w = 1 << 20;
	const a = makeArray(w);
	const b = makeBytes(w);
	const c = makeWords(w);
	for (let i = 0; i < 3; i++) {
		a.fill(true);
		sieveArray(a, w);
		b.fill(1);
		sieveBytes(b, w);
		c.fill(0);
		sieveBits(c, w);
	}
}

const rows = [];

for (const n of allSizes) {
	const arr = makeArray(n);
	const bytes = makeBytes(n);
	const words = makeWords(n);

	arr.fill(true);
	const opsArray = countArray(arr, n);
	bytes.fill(1);
	const opsBytes = countBytes(bytes, n);
	words.fill(0);
	const opsBits = countBits(words, n);

	const msArray = measure(
		() => arr.fill(true),
		() => sieveArray(arr, n)
	);
	const msBytes = measure(
		() => bytes.fill(1),
		() => sieveBytes(bytes, n)
	);
	const msBits = measure(
		() => words.fill(0),
		() => sieveBits(words, n)
	);

	rows.push({
		n,
		arrayBytes: (n + 1) * 8,
		bitsetBytes: words.length * 4,
		array: (msArray * 1e6) / opsArray,
		bytes: (msBytes * 1e6) / opsBytes,
		bits: (msBits * 1e6) / opsBits,
		msArray,
		msBytes,
		msBits,
	});

	if (process.stdout.isTTY) {
		process.stdout.write(`\r  ${rows.length} of ${allSizes.length} done`);
	}
}

if (process.stdout.isTTY) process.stdout.write("\n");
console.log();

// --------------------------------------------------------------- the numbers

const head = (text, width) => text.padStart(width);
console.log(
	`${head("n", 13)}  ${head("Array", 11)}  ${head("Uint8Array", 11)}  ${head("bitset", 11)}`
);
console.log(`${"-".repeat(13)}  ${"-".repeat(11)}  ${"-".repeat(11)}  ${"-".repeat(11)}`);

for (const r of rows) {
	console.log(
		`${head(r.n.toLocaleString(), 13)}  ${head(r.array.toFixed(3), 11)}  ${head(
			r.bytes.toFixed(3),
			11
		)}  ${head(r.bits.toFixed(3), 11)}`
	);
}

// --------------------------------------------------------------- the picture

const scale = Math.max(...rows.flatMap((r) => [r.array, r.bytes, r.bits]));

function chart(title, key) {
	console.log();
	console.log(title);
	console.log();
	for (const r of rows) {
		const filled = Math.round((48 * r[key]) / scale);
		const bar = "#".repeat(Math.min(48, Math.max(1, filled)));
		console.log(`${head(r.n.toLocaleString(), 13)}  ${bar.padEnd(48)}  ${r[key].toFixed(3)}`);
	}
	console.log();
}

chart("Cost per marking operation, new Array(n)", "array");
chart("Cost per marking operation, Uint8Array (same scale)", "bytes");
chart("Cost per marking operation, Uint32Array bitset (same scale)", "bits");

// ----------------------------------------------------------------- the point

const race = rows.find((r) => r.n === RACE);

console.log(`At the race size, n = ${RACE.toLocaleString()}:`);
console.log(
	`  new Array(n)   ${formatBytes(race.arrayBytes).padStart(9)}  ${race.array
		.toFixed(3)
		.padStart(7)} ns/op  ${race.msArray.toFixed(3).padStart(8)} ms per sieve`
);
console.log(
	`  Uint8Array     ${formatBytes(RACE + 1).padStart(9)}  ${race.bytes
		.toFixed(3)
		.padStart(7)} ns/op  ${race.msBytes.toFixed(3).padStart(8)} ms per sieve`
);
console.log(
	`  bitset         ${formatBytes(race.bitsetBytes).padStart(9)}  ${race.bits
		.toFixed(3)
		.padStart(7)} ns/op  ${race.msBits.toFixed(3).padStart(8)} ms per sieve`
);
console.log();

console.log("Across every size measured:");
const last = rows[rows.length - 1];
for (const [label, key] of [
	["new Array(n) ", "array"],
	["Uint8Array   ", "bytes"],
	["bitset       ", "bits"],
]) {
	const best = Math.min(...rows.map((r) => r[key]));
	const atEnd = last[key];
	console.log(
		`  ${label}  best ${best.toFixed(3).padStart(6)}, and ${atEnd
			.toFixed(3)
			.padStart(6)} at n = ${last.n.toLocaleString()}  (${(atEnd / best)
			.toFixed(1)
			.padStart(4)}x)`
	);
}

console.log();
console.log("Two things to take from this, and the first is the one that surprises people.");
console.log();
console.log("The Array is slower even at the smallest sizes, where everything fits in L1.");
console.log("That gap is not cache at all, it is the cost of a hole check and a type check");
console.log("on every access to a tagged array. Moving to a typed array wins twice: fewer");
console.log("bytes and a cheaper way of reaching them.");
console.log();
console.log("The bitset barely moves across the whole range, because it never gets big");
console.log("enough to leave cache. That is the point of making your data smaller.");
console.log();
console.log("This times the marking loop only. Allocating and refilling the sieve each lap");
console.log("is excluded here but is real cost in the race.");
console.log();
console.log("Single rows move by a few tens of percent between runs, the Array worst of");
console.log("all. Read the trend across the chart rather than any one line of it.");

// ------------------------------------------------------------------ the data

const csv = [
	"n,arrayBytes,bitsetBytes,nsPerOpArray,nsPerOpUint8,nsPerOpBitset,msArray,msUint8,msBitset",
	...rows.map((r) =>
		[
			r.n,
			r.arrayBytes,
			r.bitsetBytes,
			r.array.toFixed(4),
			r.bytes.toFixed(4),
			r.bits.toFixed(4),
			r.msArray.toFixed(4),
			r.msBytes.toFixed(4),
			r.msBits.toFixed(4),
		].join(",")
	),
].join("\n");

fs.writeFileSync("cache-cliff-js.csv", `${csv}\n`);
console.log();
console.log("Wrote cache-cliff-js.csv");
