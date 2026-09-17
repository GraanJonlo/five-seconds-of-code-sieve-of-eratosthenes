# Facilitator solutions

Reference implementations of every rung, one file per rung per language, plus the
script that measures them. Spoilers throughout — this lives on the `facilitator`
branch with `FACILITATOR-NOTES.md` and should never be merged to `main`.

Each file is a **complete drop-in replacement** for that race's sieve file. To try
one, copy it over the real thing and run the race as normal:

```
cp facilitator-solutions/csharp/rung-6-zero-means-prime.cs csharp/Sieve.cs
cd csharp && dotnet run -c Release
```

The public API matches the starting point exactly, so nothing else needs touching.
Every file carries a header explaining what the rung does, why it works, and what
breaks — they are written to be read out in a debrief.

## What is here

| | |
|---|---|
| `csharp/` | Rungs 1–6 cumulative, plus 7 (blocking), 8 (wheel), 9 (no bounds checks) and a pooled variant, each measured as a delta from rung 6 |
| `fsharp/` | Rungs 1–6 cumulative |
| `go/` | Rungs 2–6 cumulative — Go starts on a flat slice, so it has no rung 1 |
| `javascript/` | Rungs 1–6 cumulative |
| `measure.ps1` | Builds and races everything, writes the CSVs |
| `results.csv` | Every individual run — 5 repetitions per configuration |
| `summary.csv` | Aggregated per configuration: max, median, min rate and speedup |
| `core-sweep.csv` | The pre-flight sweep identifying which logical CPUs are E-cores |
| `replications/` | Two later full re-runs, raw and aggregated. See below |

Every language also has a `rung-6-on-byte-array` variant: rung 6 applied directly to rung 4,
skipping the bitset entirely. It exists because the nudge order in the notes depends on
knowing whether inverting the sense pays without rung 5 underneath it. It does, in every
language, and it is the only rung measured never to cost anything.

Rungs 1–6 are **cumulative**: each includes everything below it, which is the path a
pair actually climbs. Rungs 7, 8, 9 and pooling are **independent deltas from rung 6**,
because stacking them answers a question nobody asked — a team reaching rung 9 applies
it to a rung-6 bitset, not to a wheel — and because a regression at rung 7 would
otherwise contaminate everything above it.

## Re-measuring on your own machine

```
pwsh facilitator-solutions/measure.ps1
```

Takes about 25 minutes and needs the .NET SDK, Node and Go. It refuses to run if the
four sieve files have uncommitted changes. Everything happens in a scratch directory
under `%TEMP%` — the repository working tree is never modified, and your own
`baseline.txt` files are left alone.

**Do this before a session if you want to quote numbers.** Laps are not portable
between machines, and the ratios move too: they depend on cache sizes, on memory
bandwidth, and on how the runtimes on your box behave.

## The replications

`replications/` holds two further complete runs, made on a later and busier day in order to
measure the byte array variant. They are kept separate rather than merged because absolute
rates that day ran about 25% lower with noticeably worse spread, and mixing two sessions'
numbers into one table is exactly the problem these files exist to prevent. `results.csv` and
`summary.csv` remain the evidence for the tables in the notes.

They are worth keeping for a second reason. The absolute rates moved by a quarter between
days; the **ratios did not move**. Rung 5's step over rung 4 came out within about 2% in all
four languages across all three sessions, and every ratio-expressed finding in the notes
replicated. That is the strongest evidence here that these numbers describe the code rather
than the afternoon.

## Methodology

The numbers are ratios of throughput against each language's own unmodified starting
point, measured as follows.

**Everything is pre-built.** Variants are compiled into separate output directories
before any measurement starts, and the measurement loop only launches executables.
This is not tidiness. `Copy-Item` preserves timestamps, so a variant checked out of
git is often *older* than the previous rung's `Race.dll`; MSBuild's incremental check
then skips the compile and the harness silently races the wrong binary. Several rungs
tie and nothing tells you why.

**Affinity is a P-core mask, not a single core.** This machine is an i9-12900HK with 6
performance cores and 8 efficiency cores. An unpinned single-threaded race that lands
on an E-core reads 30–40% slow. But pinning to *one* core is worse than not pinning at
all: every one of these runtimes does memory management on other threads — .NET's
concurrent GC over 1 MB-per-lap large-object allocations, Go's concurrent mark, V8's
scavenger — and squeezing those onto the sieve's own core costs in proportion to
allocation volume per lap, which is exactly the quantity each rung reduces. That
inflates precisely the rungs being measured. The mask covers all 12 P-core logical
CPUs, which excludes the E-cores while leaving the collectors somewhere to run.
`GOMAXPROCS` is pinned too, because Go derives `NumCPU` from the process affinity
mask on Windows.

A pre-flight sweep races one variant on each logical CPU in turn and records the
result in `core-sweep.csv`, so the E-core split is confirmed rather than assumed.

**Runs are shuffled and repetition-major**, with the four baselines inside the
shuffle. Sequential execution over half an hour on a laptop confounds rung number
with thermal state; shuffling means drift shows up as noise in baseline and variant
alike instead of masquerading as a rung effect. The baseline observations spread
across the session also give a measured drift figure rather than a hopeful one.

**The headline statistic is the maximum rate over 5 repetitions.** Laps completed in a
fixed window is one-sided noise — interference can remove laps, nothing can add them —
so the maximum is the best available estimate of what the machine can actually do,
where a median is dragged down by whatever happened to be running. `summary.csv`
carries median and min alongside, and a `Spread` column: anything above about 1.05 is
inside the noise floor and should not be quoted as a clean number.

**Rate is laps ÷ elapsed, not laps ÷ 5.** Every harness tests its `while` condition
before each lap, so a run overruns five seconds by up to one lap — worth ~0.2% at the
slowest baselines and nothing at the fast end, but free to get right.

**Validity is asserted on every run.** A configuration that fails the self test, exits
non-zero, or reports anything other than 78,498 primes aborts the run rather than
recording a number.

## Correctness

Every variant passes the harness self test at sizes 10 through 100,000, which checks
both the count of primes and their sum.

That is not sufficient on its own, and the reference solutions were checked further.
The self test has two blind spots: every size it tests is composite, so it cannot
distinguish "primes below n" from "primes up to n"; and none is odd, so it cannot
catch an index count of `n/2` where it should be `(n+1)/2`. Both would sail through
here and then bite a team who copied the solution. Each variant was therefore verified
against an independent trial-division reference at 20 sizes including 0, 1, 2, 3, 9,
11, 29, 31, 49, 101, 127, 999, 9,973 and 100,001 — odd, prime, and either side of the
wheel and word boundaries.
