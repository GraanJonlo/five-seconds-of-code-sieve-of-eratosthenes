# 5 Seconds of Code - Sieve of Eratosthenes

The 24 Hours of Le Mans (French: 24 Heures du Mans) is an endurance-focused sports car race held annually near the town of Le Mans, France. It is the world's oldest active endurance racing event. Unlike fixed-distance races whose winner is determined by minimum time, the 24 Hours of Le Mans is won by the car that covers the greatest distance in 24 hours.

For this challenge you are presented with an algorithm for finding prime numbers, the [Sieve of Eratosthenes](https://en.wikipedia.org/wiki/Sieve_of_Eratosthenes) and have to see, in 5 seconds, how many times you can find all primes under 1,000,000. Naive implementations have been provided in C#, F#, JavaScript and Go.

Working in pairs, see how far you can optimize the code. Significant gains are possible.

## The rules

* Your solution uses the sieve of Eratosthenes
* Your solution runs for at least 5 seconds, and stops as quickly as possible after that
* Your solution calculates all the primes up to 1,000,000
* No 3rd party libraries are used, all code must be your own
* Single threaded only. No threads, goroutines, workers or SIMD intrinsics - the point
  is to make the algorithm and its memory access better, not to buy more cores
* You may only edit the sieve itself - `Sieve.cs`, `Sieve.fs`, `primeSieve.js`, `primeSieve.go`.
  The timing and validation code is off limits
* The sieve must be fully computed by the time the run step finishes. Collecting the
  results afterwards is not timed, so moving sieving work into the results step is
  scoring laps you never ran

Every race self tests at sieve sizes 10 through 100,000 before the clock starts, so an
off-by-one is reported against the size that broke it rather than as a bare
`Valid: false` five seconds later. Both the number of primes and their sum are checked -
returning the right number of wrong answers does not pass. A race that fails validation
exits with a non-zero status.

## Scoring

Laps are not comparable between machines or between languages, so each race scores you
against your own starting point. **Run your race once before you change anything.** The
first valid result is written to `baseline.txt` in that race's directory, and every run
after that reports a multiple of it:

```
Laps: 2451 Time: 5.0018 #Primes: 78498 Valid: True
Speedup: 4.02x baseline (610 laps)
```

Delete `baseline.txt` to re-record. Expect roughly 5% run to run noise on an otherwise
idle machine, so anything under about 1.1x is not a real gain - close your laptop lid on
nothing else and re-run before believing a small number.

## Prerequisites

You only need the toolchain for the race you are running.

* The C# and F# races need the .NET 10 SDK or newer. Both projects target `net10.0`,
  which gives you C# 14 and F# 10 by default
* The JavaScript race needs Node 22 or newer, and is an ES module
* The Go race needs Go 1.19 or newer

The starting points use nothing newer than C# 12 and F# 8, so you do not need the latest
syntax to read them, but you are free to use the full C# 14 and F# 10 feature set while
optimizing.

## Execution

Run each race from its own directory, so that its `baseline.txt` lands beside it.

The .NET races

```
dotnet run -c Release
```

The Node race

```
node .
```

or `node --run race`.

The Go race

```
go run .
```

## Taking things further

This exercise was inspired/ ripped off from a YouTube video from [Dave's Garage](https://www.youtube.com/watch?v=D3h62rgewZM)

If you want to take it further check out the [official race repo](https://github.com/PlummersSoftwareLLC/Primes)
