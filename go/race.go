package main

import (
	"fmt"
	"os"
	"strconv"
	"strings"
	"time"
)

// Everything in this file is off limits. primeSieve.go is the only file you may edit.

type validation struct {
	count int
	sum   int64
}

const baselineFile = "baseline.txt"

func readBaseline() (int, bool) {
	data, err := os.ReadFile(baselineFile)
	if err != nil {
		return 0, false
	}

	baseline, err := strconv.Atoi(strings.TrimSpace(string(data)))
	if err != nil || baseline <= 0 {
		return 0, false
	}

	return baseline, true
}

func sum(primes []int) int64 {
	var total int64
	for _, prime := range primes {
		total += int64(prime)
	}
	return total
}

func main() {
	// Historical data for validating our results - the number of primes to be found
	// under some limit, and the sum of those primes, such as 168 primes under 1,000
	// which sum to 76,127. The sum is checked as well as the count so that returning
	// the right number of wrong answers does not pass.
	validationData := map[int]validation{
		10:          {4, 17},
		100:         {25, 1_060},
		1_000:       {168, 76_127},
		10_000:      {1_229, 5_736_396},
		100_000:     {9_592, 454_396_537},
		1_000_000:   {78_498, 37_550_402_023},
		10_000_000:  {664_579, 3_203_324_994_356},
		100_000_000: {5_761_455, 279_209_790_387_276},
	}

	const sieveSize int = 1_000_000

	// Self test before the race. Sieving small ranges first means an off-by-one in a
	// rewritten sieve gets reported against the size that broke it, instead of showing
	// up as a bare "Valid: false" five seconds later.
	for _, size := range []int{10, 100, 1_000, 10_000, 100_000} {
		candidate := getSieve(size)
		candidate.run()

		candidatePrimes := candidate.result()
		candidateSum := sum(candidatePrimes)
		expected := validationData[size]

		if len(candidatePrimes) != expected.count || candidateSum != expected.sum {
			fmt.Printf("Self test FAILED at sieve size %d: expected %d primes summing to %d, got %d summing to %d\n",
				size, expected.count, expected.sum, len(candidatePrimes), candidateSum)
			os.Exit(1)
		}
	}

	fmt.Println("Self test passed")

	laps := 0
	sieve := getSieve(sieveSize)

	start := time.Now()
	for time.Since(start).Seconds() < 5 {
		sieve = getSieve(sieveSize)
		sieve.run()
		laps++
	}

	duration := time.Since(start).Seconds()

	primes := sieve.result()
	primeSum := sum(primes)
	expected := validationData[sieveSize]
	valid := len(primes) == expected.count && primeSum == expected.sum

	fmt.Printf("Laps: %d Time: %f #Primes: %d Valid: %t\n", laps, duration, len(primes), valid)

	if !valid {
		os.Exit(1)
	}

	// Record the first valid result as this machine's baseline, then report every later
	// run as a multiple of it. Run once before changing anything to get an honest one.
	if baseline, ok := readBaseline(); ok {
		fmt.Printf("Speedup: %.2fx baseline (%d laps)\n", float64(laps)/float64(baseline), baseline)
	} else if err := os.WriteFile(baselineFile, []byte(strconv.Itoa(laps)), 0o644); err != nil {
		fmt.Printf("Could not record baseline: %v\n", err)
	} else {
		fmt.Printf("Baseline recorded: %d laps. Now optimise primeSieve.go. Delete %s to re-record.\n",
			laps, baselineFile)
	}
}
