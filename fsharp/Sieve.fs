namespace DragRace

// This is the only file you may edit.
//
// The rules:
//   * It has to stay a sieve of Eratosthenes.
//   * create and run are inside the timed loop, getResults is not. run must leave
//     the sieve fully computed - moving sieving work into getResults is scoring
//     laps you never ran.
//   * No 3rd party libraries, all code must be your own.
module Sieve =
    let create sieveSize = Array.init sieveSize (fun _ -> true)

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
