namespace DragRace

// Rung 1 - Array.init to Array.create. F# only.
//
// Array.init sieveSize (fun _ -> true) is one million closure invocations. It calls
// a function once per element to compute a value that never varies. Array.create
// does a vectorised fill instead.
//
// Nearly free, and it is sitting right there in create.
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
        let rec run' factor candidates =
            if factor > Array.length candidates then candidates
            else if candidates.[factor - 1] then run' (factor + 1) (sieveOut factor candidates)
            else run' (factor + 1) candidates

        run' 2 candidates

    let getResults candidates =
        let rec getResults' candidates current primes =
            if current > Array.length candidates
            then primes
            else if candidates.[current - 1]
            then getResults' candidates (current + 1) (current :: primes)
            else getResults' candidates (current + 1) primes

        getResults' candidates 2 [] |> List.rev
