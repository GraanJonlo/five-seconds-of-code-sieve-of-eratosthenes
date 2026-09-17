namespace DragRace

// Rung 6 applied to rung 4, WITHOUT the bitset. Rungs 1, 2, 3, 4 and 6.
//
// Inverting the sense so that zero means prime does not need the bitset under it.
// On the odds only byte array it deletes the Array.create fill entirely, every lap,
// and cannot cost anything in exchange - it only removes work.
//
// Measured because the nudge order depends on it: rung 5 can go backwards, this
// cannot, so a pair with time left after rung 4 should be pointed here first.
//
// No tail word problem either. The array is exactly count long, so there are no
// spare slots past n waiting to be read back as prime.
module Sieve =
    type State = { Flags: bool[]; Size: int }

    let create sieveSize =
        // zeroCreate, and false now means prime - so no fill at all.
        { Flags = Array.zeroCreate ((sieveSize + 1) / 2)
          Size = sieveSize }

    let run state =
        let flags = state.Flags
        let count = flags.Length
        let limit = int (sqrt (float state.Size))
        let mutable factor = 3

        while factor <= limit do
            if not flags.[factor >>> 1] then
                let mutable q = (factor * factor) >>> 1

                while q < count do
                    flags.[q] <- true
                    q <- q + factor

            factor <- factor + 2

        state

    let getResults state =
        let primes = ResizeArray<int>()

        if state.Size >= 2 then primes.Add 2

        for i in 1 .. state.Flags.Length - 1 do
            if not state.Flags.[i] then primes.Add(2 * i + 1)

        List.ofSeq primes
