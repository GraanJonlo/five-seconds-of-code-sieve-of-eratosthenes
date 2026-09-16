# 5 Seconds of Code - Sieve of Eratosthenes

The 24 Hours of Le Mans (French: 24 Heures du Mans) is an endurance-focused sports car race held annually near the town of Le Mans, France. It is the world's oldest active endurance racing event. Unlike fixed-distance races whose winner is determined by minimum time, the 24 Hours of Le Mans is won by the car that covers the greatest distance in 24 hours.

For this challenge you are presented with an algorithm for finding prime numbers, the [Sieve of Eratosthenes](https://en.wikipedia.org/wiki/Sieve_of_Eratosthenes) and have to see, in 5 seconds, how many times you can find all primes under 1,000,000. Naive implementations have been provided in C#, F#, JavaScript and Go.

Working in pairs, see how far you can optimize the code. Significant gains are possible.

## The rules

* Your solution uses the sieve of Eratosthenes
* Your solution runs for at least 5 seconds, and stops as quickly as possible after that
* Your solution calculates all the primes up to 1,000,000
* No 3rd party libraries are used, all code must be your own
* You may only edit the sieve itself - `Sieve.cs`, `Sieve.fs`, `primeSieve.js`, `primeSieve.go`.
  The timing and validation code is off limits
* The sieve must be fully computed by the time the run step finishes. Collecting the
  results afterwards is not timed, so moving sieving work into the results step is
  scoring laps you never ran

The .NET races self test at sieve sizes 10 through 100,000 before the clock starts, so an
off-by-one is reported against the size that broke it rather than as a bare
`Valid: False` five seconds later. Both the number of primes and their sum are checked -
returning the right number of wrong answers does not pass. The Node and Go races still
only check the count, at 1,000,000, after the race.

## Prerequisites

.NET 10+, Node 18+ or Go 1.19+

## Execution

The .NET races

```
dotnet run -c Release
```

The Node race

```
node .
```

The Go race

```
go run .
```

## Taking things further

This exercise was inspired/ ripped off from a YouTube video from [Dave's Garage](https://www.youtube.com/watch?v=D3h62rgewZM)

If you want to take it further check out the [official race repo](https://github.com/PlummersSoftwareLLC/Primes)
