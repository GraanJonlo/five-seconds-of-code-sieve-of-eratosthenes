namespace DragRace

// Rung 2 - stop the outer loop at the square root. Includes rung 1.
//
// Every composite has a prime factor no larger than its own square root, so once
// the factor passes sqrt(n) there is nothing left to mark.
//
// What this removes is the outer loop's scan of all 1,000,000 entries looking for
// the next prime. The saving is a read pass, not writes.
//
// Remember this array is 1-based against the numbers: index i holds the number
// i + 1, so Array.length candidates IS n.
module Sieve =
    let create sieveSize = Array.create sieveSize true

    let sieveOut factor candidates =
        let rec markNonPrimes factor q =
            if q > Array.length candidates then
                candidates
            else
                candidates.[q - 1] <- false
                markNonPrimes factor (q + factor)

        markNonPrimes factor (factor + factor)

    let run candidates =
        let limit = int (sqrt (float (Array.length candidates)))

        let rec run' factor =
            if factor > limit then
                candidates
            else
                if candidates.[factor - 1] then
                    sieveOut factor candidates |> ignore

                run' (factor + 1)

        run' 2

    let getResults candidates =
        let rec getResults' candidates current primes =
            if current > Array.length candidates
            then primes
            else if candidates.[current - 1]
            then getResults' candidates (current + 1) (current :: primes)
            else getResults' candidates (current + 1) primes

        getResults' candidates 2 [] |> List.rev
