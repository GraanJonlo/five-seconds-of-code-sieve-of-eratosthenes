using System;
using System.Collections.Generic;
using System.Diagnostics;
using Race;

// Everything in this file is off limits. Sieve.cs is the only file you may edit.

// Historical data for validating our results - the number of primes to be found
// under some limit, and the sum of those primes, such as 168 primes under 1,000
// which sum to 76,127. The sum is checked as well as the count so that returning
// the right number of wrong answers does not pass.
Dictionary<int, (int Count, long Sum)> validationData = new()
{
	{10, (4, 17L)},
	{100, (25, 1_060L)},
	{1_000, (168, 76_127L)},
	{10_000, (1_229, 5_736_396L)},
	{100_000, (9_592, 454_396_537L)},
	{1_000_000, (78_498, 37_550_402_023L)},
	{10_000_000, (664_579, 3_203_324_994_356L)},
	{100_000_000, (5_761_455, 279_209_790_387_276L)}
};

const int sieveSize = 1_000_000;

// Self test before the race. Sieving small ranges first means an off-by-one in a
// rewritten Sieve gets reported against the size that broke it, instead of showing
// up as a bare "Valid: False" five seconds later.
foreach (int size in new[] { 10, 100, 1_000, 10_000, 100_000 })
{
	Sieve candidate = new Sieve(size);
	candidate.Run();
	List<int> candidatePrimes = candidate.Result();

	(int expected, long expectedSum) = validationData[size];
	long candidateSum = 0;
	foreach (int prime in candidatePrimes)
	{
		candidateSum += prime;
	}

	if (candidatePrimes.Count != expected || candidateSum != expectedSum)
	{
		Console.WriteLine(
			$"Self test FAILED at sieve size {size}: expected {expected} primes summing to {expectedSum}, got {candidatePrimes.Count} summing to {candidateSum}");
		return 1;
	}
}

Console.WriteLine("Self test passed");

int laps = 0;
Sieve? sieve = null;

var stopwatch = Stopwatch.StartNew();

while (stopwatch.Elapsed.TotalSeconds < 5)
{
	sieve = new Sieve(sieveSize);
	sieve.Run();
	laps++;
}

stopwatch.Stop();

List<int> primes = sieve != null ? sieve.Result() : [];

long primeSum = 0;
foreach (int prime in primes)
{
	primeSum += prime;
}

(int expectedPrimeCount, long expectedPrimeSum) = validationData[sieveSize];
bool valid = primes.Count == expectedPrimeCount && primeSum == expectedPrimeSum;

Console.WriteLine(
	$"Laps: {laps} Time: {stopwatch.Elapsed.TotalSeconds} #Primes: {primes.Count} Valid: {valid}");

return valid ? 0 : 1;
