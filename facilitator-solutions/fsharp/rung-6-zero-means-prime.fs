namespace DragRace

// Rung 6 - stop refilling the array. Includes rungs 1 to 5.
//
// Array.zeroCreate hands back zeroed memory. Invert the sense so that ZERO means
// prime and the fill disappears entirely - a whole pass over the array, deleted,
// every lap. The runtime was already zeroing it; we stopped fighting that.
//
// THE TAIL WORD BUG. The array holds ceil(count / 64) words, so the last word has
// bits for numbers past n. Those bits are zero, and zero now means prime. At
// n = 1,000,000 that is 32 spare bits covering 1,000,001 to 1,000,063, of which
// 1,000,003 is prime - so the sieve reports 78,499 primes and fails validation.
//
// Bound getResults by count, never by Words.Length * 64. This only bites once rungs
// 5 and 6 are BOTH present, which in a cumulative ladder means it shows up exactly
// once, at the variant you are most likely to glance at and call obviously fine.
module Sieve =
    type State = { Words: uint64[]; Size: int }

    let create sieveSize =
        let count = (sieveSize + 1) / 2
        { Words = Array.zeroCreate ((count + 63) / 64)
          Size = sieveSize }

    let run state =
        let words = state.Words
        let count = (state.Size + 1) / 2
        let limit = int (sqrt (float state.Size))
        let mutable factor = 3

        while factor <= limit do
            let f = factor >>> 1

            if words.[f >>> 6] &&& (1UL <<< f) = 0UL then
                let mutable q = (factor * factor) >>> 1

                while q < count do
                    words.[q >>> 6] <- words.[q >>> 6] ||| (1UL <<< q)
                    q <- q + factor

            factor <- factor + 2

        state

    let getResults state =
        let count = (state.Size + 1) / 2
        let primes = ResizeArray<int>()

        if state.Size >= 2 then primes.Add 2

        // Bounded by count, not by Words.Length * 64 - see the tail word note above.
        for i in 1 .. count - 1 do
            if state.Words.[i >>> 6] &&& (1UL <<< i) = 0UL then primes.Add(2 * i + 1)

        List.ofSeq primes
