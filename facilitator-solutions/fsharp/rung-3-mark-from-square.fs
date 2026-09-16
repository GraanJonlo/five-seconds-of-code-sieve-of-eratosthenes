namespace DragRace

// Rung 3 - start marking at f*f, not 2f. Includes rungs 1 and 2.
//
// Any multiple k*f where k < f was already marked when that smaller factor had its
// turn. For the factor 7, everything from 14 through 42 was covered by 2, 3 and 5.
// The first multiple of 7 that nothing else has reached is 49.
module Sieve =
    let create sieveSize = Array.create sieveSize true

    let sieveOut factor candidates =
        let rec markNonPrimes factor q =
            if q > Array.length candidates then
                candidates
            else
                candidates.[q - 1] <- false
                markNonPrimes factor (q + factor)

        markNonPrimes factor (factor * factor)

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
