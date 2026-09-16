namespace DragRace

// Rung 4 - odds only. Includes rungs 1 to 3.
//
// Index i now represents the number 2i + 1, so the array holds only the odd
// candidates: half the operations and half the footprint.
//
// F# HAS TO CHANGE THE REPRESENTATION HERE, and this is the trap. The starting
// point recovers n from Array.length in three places, which works only while index
// i means number i + 1. Odds only breaks that identity, and at rung 5 it becomes
// unrecoverable - a uint64[] of 7,813 words cannot tell you whether n was 1,000,000
// or 1,000,063. So the sieve has to carry its own size.
//
// Program.fs never annotates a type and only calls create, run and getResults, so
// changing the representation is legal. Three constraints it does impose:
//   * create 0 is called before the timing loop, so it must work at zero
//   * run must return the sieve, because Program.fs pipes create into run
//   * getResults must return an F# list - Program.fs calls List.length and List.sumBy
//
// The loops go imperative from here. The fastest F# sieve is a mutable array in a
// for loop and looks like C# in a false moustache; reaching for folds and Seq here
// goes backwards.
module Sieve =
    type State = { Flags: bool[]; Size: int }

    let create sieveSize =
        { Flags = Array.create ((sieveSize + 1) / 2) true
          Size = sieveSize }

    let run state =
        let flags = state.Flags
        let count = flags.Length
        let limit = int (sqrt (float state.Size))
        let mutable factor = 3

        while factor <= limit do
            if flags.[factor >>> 1] then
                // Even multiples are already gone, so the stride is 2*factor in
                // number space, which is exactly factor in index space.
                let mutable q = (factor * factor) >>> 1

                while q < count do
                    flags.[q] <- false
                    q <- q + factor

            factor <- factor + 2

        state

    let getResults state =
        let primes = ResizeArray<int>()

        // 2 is the one prime an odds only array cannot represent.
        if state.Size >= 2 then primes.Add 2

        // Index 0 is the number 1, which is not prime.
        for i in 1 .. state.Flags.Length - 1 do
            if state.Flags.[i] then primes.Add(2 * i + 1)

        List.ofSeq primes
