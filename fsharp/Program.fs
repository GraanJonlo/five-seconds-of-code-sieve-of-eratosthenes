namespace DragRace

module Main =
    open System.Diagnostics

    // Everything in this file is off limits. Sieve.fs is the only file you may edit.

    // Historical data for validating our results - the number of primes to be found
    // under some limit, and the sum of those primes, such as 168 primes under 1,000
    // which sum to 76,127. The sum is checked as well as the count so that returning
    // the right number of wrong answers does not pass.
    let validationData =
        [ 10, (4, 17L)
          100, (25, 1_060L)
          1_000, (168, 76_127L)
          10_000, (1_229, 5_736_396L)
          100_000, (9_592, 454_396_537L)
          1_000_000, (78_498, 37_550_402_023L)
          10_000_000, (664_579, 3_203_324_994_356L)
          100_000_000, (5_761_455, 279_209_790_387_276L) ]
        |> Map.ofList

    let sieveSize = 1_000_000

    // Self test before the race. Sieving small ranges first means an off-by-one in a
    // rewritten Sieve gets reported against the size that broke it, instead of showing
    // up as a bare "Valid: false" five seconds later.
    let selfTest () =
        [ 10; 100; 1_000; 10_000; 100_000 ]
        |> List.tryPick (fun size ->
            let primes = Sieve.create size |> Sieve.run |> Sieve.getResults
            let expected, expectedSum = validationData.[size]
            let actual = List.length primes
            let actualSum = primes |> List.sumBy int64

            if actual = expected && actualSum = expectedSum then
                None
            else
                Some(
                    sprintf "Self test FAILED at sieve size %d: expected %d primes summing to %d, got %d summing to %d"
                        size expected expectedSum actual actualSum))

    [<EntryPoint>]
    let main _ =
        match selfTest () with
        | Some failure ->
            printfn "%s" failure
            1
        | None ->
            printfn "Self test passed"

            let mutable laps = 0
            let mutable candidates = Sieve.create 0
            let stopwatch = Stopwatch.StartNew()

            while stopwatch.Elapsed.TotalSeconds < 5.0 do
                candidates <- Sieve.create sieveSize |> Sieve.run
                laps <- laps + 1

            stopwatch.Stop()

            let primes = Sieve.getResults candidates
            let expected, expectedSum = validationData.[sieveSize]

            let isValid =
                List.length primes = expected
                && (primes |> List.sumBy int64) = expectedSum

            printfn "Laps: %d Time: %f #Primes: %d Valid: %b" laps stopwatch.Elapsed.TotalSeconds
                (List.length primes) isValid

            if isValid then 0 else 1
