namespace DragRace

// Rung 5 - bit packing. Includes rungs 1 to 4.
//
// One bit per odd candidate in 64-bit words. Eight times smaller again, sixteen
// times smaller than the original.
//
//     word index    i >>> 6
//     bit within    i &&& 63
//     test          words.[i >>> 6] &&& (1UL <<< i) <> 0UL
//     clear         words.[i >>> 6] <- words.[i >>> 6] &&& ~~~(1UL <<< i)
//
// .NET masks the shift count, so 1UL <<< i behaves as 1UL <<< (i &&& 63).
//
// A set bit still means prime here, so the words are filled with ones and the tail
// word's spare bits are set too. getResults is bounded by count, so they are never
// read. Rung 6 inverts the sense, and that is where the tail word bites.
module Sieve =
    type State = { Words: uint64[]; Size: int }

    let create sieveSize =
        let count = (sieveSize + 1) / 2
        { Words = Array.create ((count + 63) / 64) System.UInt64.MaxValue
          Size = sieveSize }

    let run state =
        let words = state.Words
        let count = (state.Size + 1) / 2
        let limit = int (sqrt (float state.Size))
        let mutable factor = 3

        while factor <= limit do
            let f = factor >>> 1

            if words.[f >>> 6] &&& (1UL <<< f) <> 0UL then
                let mutable q = (factor * factor) >>> 1

                while q < count do
                    words.[q >>> 6] <- words.[q >>> 6] &&& ~~~(1UL <<< q)
                    q <- q + factor

            factor <- factor + 2

        state

    let getResults state =
        let count = (state.Size + 1) / 2
        let primes = ResizeArray<int>()

        if state.Size >= 2 then primes.Add 2

        for i in 1 .. count - 1 do
            if state.Words.[i >>> 6] &&& (1UL <<< i) <> 0UL then primes.Add(2 * i + 1)

        List.ofSeq primes
