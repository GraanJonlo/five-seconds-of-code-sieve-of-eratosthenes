# Facilitator notes

> **Spoilers.** This file is the answer key. If you would rather teams did not read it,
> keep it on a branch of its own and hand out the parts you want:
>
> ```
> git checkout -b facilitator-notes
> git rm --cached FACILITATOR-NOTES.md   # on main
> ```

## What the exercise is actually for

Not "who writes the fastest sieve". The lesson is that a loop which looks compute bound is
usually memory bound, and that the fix is almost always *make the data smaller* rather than
*make the code cleverer*. Everything else is scaffolding for that one idea.

Scoring is deliberately each team against their own baseline, so nobody is competing with a
teammate who happens to have a newer laptop.

## Shape of the session

| | |
|---|---|
| Intro and rules | Everyone records a baseline, **best of three**. Worth getting right — see below |
| First rung together | Mob **rung 2 only**, the square root bound. Bank the number and stop |
| Pairs | The long stretch, with **rung 4 as the target**. Circulate, nudge, resist fixing it for them |
| Debrief | Each pair says which rung gave them the most, and what surprised them |

**Mob rung 2, and only rung 2.** This page used to say mob the square root bound and `f*f`
together. Don't. `f*f` is worth 1.08x where the square root bound is worth 2.19x to 6.02x
depending on language, so bundling them buries the big win inside a rung that does almost
nothing — and quietly teaches the room that rungs are roughly interchangeable. They are
not: see [the measured numbers](#measured-numbers).

Rung 1 is a poor thing to mob for a different reason. It is the only rung that is not the
same change in every language, so half the room watches the other half. Let pairs pick it
up on their own afterwards.

Mobbing still matters more than it sounds. It puts a real number on the board inside twenty
minutes, gives everyone shared vocabulary, and removes the cold start that makes people
feel stupid.

**Then say out loud that rung 4 is the target.** Rungs 2 and 4 between them are 10.7x of
C#'s 11.9x. Everything else on the ladder is a few percent either way, and two rungs are
negative in some languages. A pair who reaches odds only has had the session; a pair who
reaches odds only and then measures the bitset carefully has had a better one.

## Recording a baseline you can trust

The scoring is at its most fragile in the first ten minutes, and nobody notices.

`baseline.txt` captures the **first valid run, and never overwrites it**. That run happens
while the whole room is restoring packages, building and racing simultaneously — the worst
contention of the day. Whatever it records becomes the denominator of every number that
team quotes for the rest of the session.

The size of this is not hypothetical. While measuring the numbers on this page, one
repetition ran 2.3% slow across the board; inside it the Go baseline dropped **25%** and one
F# variant dropped 22%. A pair whose baseline lands 25% low spends the afternoon reading
1.25x too high and reports a speedup they never earned.

**The fix is thirty seconds in the briefing.** Everyone runs the race three times, deleting
`baseline.txt` between each, then hand-writes the highest lap count into the file. It holds
a single integer and nothing else. The README walks participants through it.

**Why the best rather than the average.** Laps completed in a fixed five seconds is
one-sided noise: interference can only ever remove laps, never add them, because nothing
makes a machine complete more laps than it is capable of. So the fastest of several runs is
the best estimate of the machine, where an average estimates the machine *plus whatever else
was running*. It is the same argument behind every number on this page being a maximum over
five repetitions rather than a median.

Say this to the room rather than just imposing the procedure. It is the first piece of
measurement discipline they need, it costs one sentence, and a team who take it on board at
the baseline will apply it to their own rungs later — which is where it actually decides
whether they believe a result.

## The machines you are measuring on

The README warns of roughly 5% run to run noise. That is right for a quiet machine and badly
understates the hazard on a modern laptop.

**Performance and efficiency cores differ by a factor of two.** Racing the same sieve pinned
to each logical CPU in turn, on the machine these numbers came from:

| Logical CPUs | | Laps per second |
|---|---|---|
| 0–11 | 6 performance cores | ~1940 |
| 12–19 | 8 efficiency cores | 950–980 |

Nothing pins a single threaded race to a fast core. A run that lands on an efficiency core
reads **half speed**, and to the pair who just made a change it looks exactly like a
regression they caused.

**What it looks like in the room.** A pair reports that a change made things much worse,
cannot see why, and reverting does not bring the old number back either. Get them to run the
same unchanged binary three times. If the spread is large, they are measuring the scheduler
rather than their code, and the answer is to take the best of several runs — the same
discipline as the baseline.

`facilitator-solutions/measure.ps1` carries the affinity pinning pattern if anyone wants to
chase it properly, including why it masks the performance cores rather than pinning to a
single one. Good aside for a pair who are flying; a rabbit hole for anybody else.

**Tell them to plug in, too.** On battery the clock is throttled and drifts as the machine
warms up, so a pair's morning numbers and their afternoon numbers are not on the same
scale.

## The ladder

Ordered by payoff per unit of effort, at n = 1,000,000. The numbers are measured — see
[Measured numbers](#measured-numbers) for the methodology and the other three languages.
"Step" is what that rung alone buys, on top of everything below it, in C#.

| # | Rung | Step | What it buys |
|---|---|---|---|
| 0 | Baseline | — | — |
| 1 | Growable list to fixed array | **1.94x** | Removes ~20 reallocations and the copying with them |
| 2 | Stop the outer loop at sqrt(n) | **2.42x** | Removes a **full sequential read of the whole array**, 1M reads down to 1000 |
| 3 | Start marking at `f*f`, not `2f` | **1.08x** | Removes genuinely redundant writes — but far fewer than anyone expects |
| 4 | Odds only | **2.27x** | Halves operations *and* footprint. First rung with an index mapping, so the first one that breaks |
| 5 | Bit packing | **0.95x** | 8x smaller again, 16x combined. Trades instructions for footprint — and at this n, loses |
| 6 | Stop refilling the array | **1.08x** | Invert the sense so zero means prime, and zero initialisation does the work free |
| 7 | Cache blocking | **0.99x** | Sieve one L1 sized block with every prime before moving on. Does nothing here |
| 8 | Wheel factorization (mod 30, mod 210) | **4.01x** | 8 residues per 30 integers instead of 15. The biggest rung on the board |
| 9 | The micro stuff | **0.96x** | Bounds check removal and unrolling. Costs more than it pays |

Rungs 7, 8 and 9 are measured against rung 6 individually rather than stacked — see
[past the ladder](#past-the-ladder).

Rungs 1, 2 and 3 together are about five lines of change and are worth **5.09x** in C#.
They are the mob session, and they are a bigger share of the total than the ordering
above suggests: by the end of rung 2 a C# pair is already at 4.70x.

### Rung 1 — growable list to fixed array

C# only. JavaScript has an equivalent that is even bigger, see the language section.

```
_sieve = [];                                  _sieve = new bool[sieveSize + 1];
for (...) _sieve.Add(true);          ->       Array.Fill(_sieve, true);
```

**Why it works.** `List<T>.Add` doubles capacity when it runs out, so building a million
entries means about twenty reallocations, and every one of them copies everything written
so far. That is roughly two million element copies before the sieve has done any work at
all. The indexer is also a method call carrying its own bounds check rather than a direct
array access.

**Watch for.** `new bool[n]` is zero initialised, which means *false*, and the naive code
treats true as prime. A pair who swaps the type but forgets to fill will get "expected 4
primes summing to 17, got 0 summing to 0" from the self test at size 10. That is the fix
working, not a mystery. The elegant answer is to invert the sense instead, which is rung 6.

**Measured: 1.94x in C#, 2.29x in JavaScript.** Nearly a doubling for a two line change,
and the largest single step either language gets before the square root bound.

### Rung 2 — stop the outer loop at the square root

Every language.

```
for (f = 2; f <= sieveSize; f++)     ->       limit = (int)Math.Sqrt(sieveSize);
                                              for (f = 2; f <= limit; f++)
```

**Why it works.** Every composite number has a prime factor no larger than its own square
root. So once the factor passes √n, every composite below n has already been marked by
something smaller, and the rest of the loop is pure waste.

**This is not the rung people think it is.** It removes almost no *marking*. What it removes
is the outer loop's scan of all 1,000,000 entries looking for the next prime — a full
megabyte streamed through cache to discover nothing. The saving is a read pass, not writes.

**Watch for.** Two things. Writing the condition as `f * f <= sieveSize` is fine at a million
but overflows `int` above about 46,341, so hoisting `limit` out of the loop is both faster
and safer. And a pair who bounds the loop at `n / 2` rather than `√n` has found something
real but far weaker — ask them why half, and let them get to the square root themselves.

**Measured: 2.42x in C#, 2.17x in F#, 2.22x in Go, 2.63x in JavaScript.** The most
uniformly valuable rung on the board, and the only one that pays about the same
everywhere. If a pair takes one thing from the session, this is the one.

### Rung 3 — start marking at `f*f`, not `2f`

Every language.

```
int q = factor + factor;             ->       int q = factor * factor;
```

**Why it works.** Any multiple `k * f` where `k < f` has a factor smaller than `f`, so it was
already marked when that smaller factor had its turn. For the factor 7, everything from 14
through 42 was covered by 2, 3 and 5. The first multiple of 7 that nothing else has reached
is 49.

**A legitimate half step.** For an odd factor, the even multiples are already gone, so the
inner loop can step `q += 2 * factor` instead of `q += factor` while still using a full
array. That halves the marking work without any of the index remapping that rung 4 demands.
It is a good place for a nervous pair to stop and take a win.

**Measured: 1.08x in C#, 1.06x in F#, 1.08x in JavaScript, 0.99x in Go.** This is the
smallest rung on the ladder, and it is worth knowing that before you send a pair after
it. Once the square root bound is in, the outer loop only reaches 1000, so the writes
this removes are the multiples `2f` through `f*f` for each prime — about 76,000 writes
against roughly 2.5 million. Three percent of the work.

It is still worth teaching, because the *reasoning* is the same reasoning that gets you
to rung 4, and it costs one character. Just do not promise a pair a big number for it.

### Rung 4 — odds only

Every language. This is where the exercise gets real, and where everybody breaks something.

Store only the odd numbers. Index `i` represents the number `2i + 1`.

```
number -> index      i = x >> 1              (for odd x)
index  -> number     x = 2 * i + 1
how many indices     (n + 1) / 2
first mark for p     (p * p) >> 1
step between marks   p                       (not 2p — in index space it halves)
```

**Why it works.** Two is the only even prime. Half of what the sieve stores can never be
prime, so it is half the memory and half the marking for free.

**Watch for.** The step is the one that gets everybody. Consecutive odd multiples of `p`
differ by `2p` as *numbers*, which is `p` as *indices*. A pair who writes `q += 2 * p` in
index space will skip half the composites and report too many primes. Remember also that 2
itself is no longer in the array and has to be added back in the results.

Expect the self test to fail here. That is what it is for. Point them at the size it names
and get them to work it by hand — at size 10 there are five indices representing 1, 3, 5, 7,
9 and it takes two minutes on paper.

**Measured: 2.27x in C#, 2.47x in F#, 2.34x in Go, 2.27x in JavaScript.** The second
biggest rung on the ladder after the square root bound, and unlike rung 5 it pays in every
language. If a pair only has time for one hard rung, this is the one — and it is the point
at which most teams stop, which is why the debrief matters.

### Rung 5 — bit packing

Every language, with a real divergence in the shift behaviour, below.

One bit per odd candidate, packed into machine words. Eight times smaller again, sixteen
times smaller than the original.

```
word index    i >> 6        (>> 5 for 32-bit words)
bit within    i & 63        (& 31 for 32-bit words)
test          (words[i >> 6] & (1UL << i)) != 0
set           words[i >> 6] |= 1UL << i
```

**What it is meant to buy.** Not less work — less memory touched. Whether that is worth
anything once the data already fits in cache is the interesting part, and at n = 1,000,000
the answer is mostly no.

**This one is counterintuitive, and the surprise is not the one this page used to
claim.** A bit write is a read, a shift, an or and a write where a byte write is a single
store, so the bitset is slower per operation — `cache-cliff` measures 0.825 ns against
0.626 ns in C#. The old claim here was that it nonetheless wins about 2x overall. It does
not. At n = 1,000,000 it is a **wash at best and a regression at worst**:

| | C# | F# | Go | JavaScript |
|---|---|---|---|---|
| Rung 5 against rung 4 | **0.95x** | **1.12x** | **1.30x** | **0.96x** |

The reason is that rung 4 already got the working set down to 500 KB, which fits in this
machine's 1.25 MB L2. Shrinking it again to 61 KB moves it from L2 to L1, which is worth
much less than the first move was, and the extra instructions per write have to be paid
out of that smaller gain. Where the language's byte access was already cheap — C# and
JavaScript — the instructions win and the rung goes backwards.

**So a pair who benchmarks the bitset, finds it slower, and abandons it has measured
correctly AND reasoned correctly.** Do not talk them out of it. The honest lesson is that
"make the data smaller" stops paying once the data already fits, and the way to know is to
measure rather than to assume. That is a better conversation than the one this page used
to recommend.

Rung 5 is still worth doing as a stepping stone: it is what makes rung 6 free, and rungs
5 and 6 together beat rung 4 in three languages out of four.

**Language divergence — shift masking.** C# and JavaScript mask the shift count
automatically, so `1UL << i` in C# behaves as `1UL << (i & 63)` and `1 << i` in JavaScript
behaves as `1 << (i & 31)`. **Go does not.** In Go a shift count at or beyond the word width
produces zero, silently, so Go needs `1 << (uint(i) & 63)` written out. A Go pair whose
bitset "nearly works" is probably hitting exactly this.

**Language divergence — word size.** JavaScript's bitwise operators are 32 bit. The bitset
wants `Uint32Array` with `>>> 5` and `& 31`. `BigUint64Array` drags in BigInt and is slower,
not faster.

### Rung 6 — stop refilling the array

Every language, and the cheapest win on the board once someone sees it.

**Invert the sense so that zero means prime.** `new bool[n]` in C#, `make([]bool, n)` in Go
and `new Uint8Array(n)` in JavaScript all hand back zeroed memory. If false means prime, the
fill loop disappears entirely — a whole pass over the array, deleted, every lap. For a
bitset this falls out naturally, since a fresh word of zeroes already means "all prime".

**The other flavour is reuse**, keeping one buffer across laps and clearing it rather than
allocating. That is faster still, and it is a rules call — see the rules section.

**Watch for.** The results step has to flip too. A pair who inverts the sieve but not the
reader gets exactly the primes and composites the wrong way round, and the self test will
report a wildly wrong count rather than an off-by-one.

**Watch for, harder — the tail word.** This is the bug that actually costs people the
afternoon, and it only appears once rungs 5 and 6 are *both* in. The word array holds
`ceil(count / 64)` words, so the final word carries bits for numbers past n. Those bits
are zero, and zero now means prime. At n = 1,000,000 that is 32 spare bits covering
1,000,001 to 1,000,063, of which 1,000,003 is prime — so the sieve reports 78,499 and
fails. Rung 5 on its own is safe because it fills with ones; a byte array is safe because
it has no padding. Bound the reader by the candidate count, never by `words.Length * 64`.

**Measured: 1.08x in C#, 1.04x in F#, 1.02x in Go, 1.00x in JavaScript.** Modest on its
own, but it is what makes rung 5 pay its way — in C# rungs 5 and 6 together are 1.03x
against rung 4, where rung 5 alone was 0.95x.

**This rung does not need the bitset under it.** Inverting the sense works just as well on
rung 4's byte array, where it is worth 1.04x to 1.33x depending on language and cannot cost
anything at all. That makes it the safest thing to point a pair at once they have rung 4,
and it changes the nudge order — see
[rung 6 without the bitset](#rung-6-without-the-bitset).

### Rung 7 — cache blocking

Every language, and usually not worth it here.

Instead of striding each prime across the whole array in turn, walk the array in blocks
sized to fit L1, and within each block mark the multiples of every prime before moving on.
Each prime needs to remember where it got to.

**Why it pays less here than people expect.** Once a pair is on an odds only bitset the whole
working set is 61 KiB and already sits comfortably in L2, so there is not much left to win.
Blocking is the right answer at a hundred million, not at a million. Say this out loud
before somebody spends an hour on it.

**Measured: 0.99x against rung 6 in C#.** Not a small win — no win at all, and slightly
negative once the per-block bookkeeping is paid for. This is now the strongest thing on
this page, because it is a measurement rather than a prediction: if a pair wants to do it
anyway, let them, but tell them the number first and let them choose.

### Rung 8 — wheel factorization

The residues coprime to 30 are 1, 7, 11, 13, 17, 19, 23 and 29 — eight numbers in every
thirty, against fifteen in thirty for odds only. That is another 1.9x reduction in space, and
mod 210 takes it to 48 in 210.

The complexity goes up sharply: marking patterns differ per residue class, and the index
arithmetic stops being a single shift. Realistically this is still a rung to *mention* in
the debrief rather than one anybody reaches in an afternoon.

**Measured: 4.01x against rung 6 in C#, taking the full ladder to 47.68x.** This page used
to call it "a comparable win" to rung 6 for far more effort. That was badly wrong. It is
**the biggest single rung in the exercise by a factor of four**, and nothing else on the
ladder is close.

**Why it is so much larger than the 1.9x the space saving predicts.** Two things compound.
The candidate count drops from 15 in 30 to 8 in 30, which is the 1.9x. But the marking loop
also gets *cheaper per operation*, which is the part nobody predicts: for a fixed prime and
a fixed residue class the bit position never changes, so the mask is loop invariant and the
inner loop collapses to

```
data[byteIdx] |= mask;      // mask hoisted out of the loop
byteIdx += p;
```

against the bitset's `words[q >> 6] |= 1UL << q`, which recomputes a variable shift on
every single write. Losing the variable shift is worth roughly as much as halving the
candidates.

This is worth a minute in the debrief even though nobody will have built it, because it is
the one rung where "touch less memory" and "do less work per touch" line up instead of
trading off — which is the exact tension rung 5 loses to.

### Rung 9 — the micro stuff

Only worth it after everything above. Bounds check removal in C#, since the `q += factor`
stride is not a shape the JIT can prove safe. Unrolling the inner loop over the eight bit
positions a given prime touches within a byte, which repeat on a fixed cycle.

**Pointers are not available here, and this page used to say they were.** `Race.csproj`
sets no `<AllowUnsafeBlocks>`, and the csproj is off limits, so `fixed` and `ulong*` will
not compile under the exercise's own rules. What does work is

```
Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_words), q >> 6) |= 1UL << q;
```

because `Unsafe.Add` takes a `ref` rather than a pointer. `Span<T>` indexing still bounds
checks, so reaching for `Span` is not the rung.

**Measured: 0.96x against rung 6 in C#.** A regression. The bounds check was not the
bottleneck — the memory traffic and the variable shift were — and removing it costs the JIT
some of the range information it was using. Treat "both are worth a few tens of percent",
which is what this page used to claim, as retired.

## Where the languages stop sharing a ladder

Baselines measured on one machine, back to back, and re-measured for the numbers on this
page — see [Measured numbers](#measured-numbers). Laps are per five second race.

| Language | Baseline laps | Starts with | Has a rung 1? |
|---|---|---|---|
| Go | 1344 | `[]bool` | No, already flat |
| F# | 1245 | `bool[]` | Nominally — `Array.init` to `Array.create`, but it measures **1.00x** |
| C# | 650 | `List<bool>` | **Yes, 1.94x** |
| JavaScript | 422 | `new Array(n)` | **Yes, 2.29x** — the biggest rung either language gets before the square root bound |

### C#

The `List<bool>` to `bool[]` move is free money nobody else gets. After that, two things
specific to .NET:

**The Large Object Heap.** `bool[1_000_001]` is 1 MB, over the 85,000 byte LOH threshold, so
the naive sieve makes a large object allocation *every lap*. An odds only bitset is 61 KiB
and allocates on the normal gen0 path. That is a cliff with nothing to do with CPU cache,
and it is invisible unless you know the threshold exists. It means C# rewards rung 5 more
than the other languages do.

**Bounds checks survive here.** The JIT eliminates them for recognisable
`for (i = 0; i < arr.Length; i++)` shapes. The sieve's `q += factor` stride is not one, so
every write pays for a check. Removing it needs `Unsafe.Add` or pointers, which is a fair
late rung.

**Measured: a full odds only bitset is 11.89x against the C# baseline**, and 12.02x if you
allow a pooled buffer.

Note that C# does **not** get the largest headline number, despite starting furthest back —
JavaScript does, at 14.08x. And the LOH story above, while real, is not what decides rung 5:
C# is one of the two languages where bit packing measures as a *regression*. See rung 5.

### F#

`Array.init sieveSize (fun _ -> true)` is **one million closure invocations**, where
`Array.create sieveSize true` does a vectorised fill. It is an F# only rung and it is
sitting right there in `Sieve.create`, so it looks like free money.

**Measured: 1.00x. It buys nothing at all.** The JIT inlines that closure and the fill ends
up costing the same either way. Worth knowing before you send an F# pair after it as their
opening move — send them at the square root bound instead, which is 2.17x.

It is still worth *showing* in the debrief, because "the obvious inefficiency was already
being optimised away, and the only way to find that out was to measure" is exactly the
lesson the session is for.

The real F# problem is cultural, not technical. The fastest F# sieve is a mutable array in a
`for` loop and looks like C# in a false moustache. A pair reaching for idiomatic immutable
F#, folds, `Seq`, list comprehensions, will go backwards and feel bad about it. Say this out
loud at the start.

### Go

Starts strongest and has the shortest ladder, so it will post the **smallest multiplier**.
Measured at **6.82x**, against JavaScript's 14.08x for the same insights. Make sure the team
knows that is the starting point's fault, not theirs.

Go is also the language that rewards bit packing most — **1.30x** for rung 5, where C# and
JavaScript both go backwards.

Its distinctive win is rung 6. `make([]bool, n)` is already zeroed and the starting point
then loops over the whole thing writing `true`. Inverting the sense so `false` means prime
deletes that entire pass. The same trick works in C#; it is just most obviously wasteful in
Go.

### JavaScript

Measured, because this one is not obvious. `new Array(n)` filled in a loop is
`HOLEY_ELEMENTS` holding tagged values:

| Representation | Footprint at n = 1,000,000 | Bytes per candidate |
|---|---|---|
| `new Array(n)`, filled in a loop | 7.64 MiB | **8.01** |
| `Uint8Array(n)` | 977 KiB | 1.00 |
| `Uint32Array` bitset, odds only | 61 KiB | 0.06 |

So the JavaScript starting point carries **eight times** the memory of the Go and C# ones for
the same sieve. That is most of why it is the slowest baseline of the four.

Cost per marking operation, measured:

| n | `Array` | `Uint8Array` | bitset |
|---|---|---|---|
| 32,768 | 1.94 | 0.99 | 1.58 |
| 1,000,000 | 1.93 | 0.72 | 1.06 |
| 16,777,216 | 4.23 | 1.37 | 1.02 |

Two things to take from that. First, `Array` is slower **even at 32 KiB, which fits in L1** —
so this is not only a cache story, it is the hole check and type check on every tagged
access. Second, the bitset in JavaScript is essentially **flat across the whole range**,
1.0x from smallest to largest, because 61 KiB never leaves cache.

The consequence is that JavaScript's ladder is reordered. `Array` to `Uint8Array` is rung 1
and worth **2.29x** on its own.

The bitset does not merely add little — **it takes it away**. Rung 5 measures **0.96x** and
rung 6 does not recover it, so JavaScript's best result in rungs 1 to 6 is rung 4, the odds
only `Uint8Array`, at **14.73x**; the full bitset ladder lands slightly behind at 14.08x.
`Uint8Array` is already fast and already small enough, and the 32-bit shift-and-mask work
costs more than the extra shrink saves.

A JavaScript pair who reports that the bitset made things worse has done the exercise
correctly. Take the result seriously in front of the room.

One more divergence: **JavaScript bitwise operators are 32 bit**, so a bitset wants
`Uint32Array` with `>>> 5` and `& 31`, not the 64 bit word layout the .NET versions use.
`BigUint64Array` involves BigInt and is slower, not faster.

## Measured numbers

Everything below was measured in one session on one machine. Reference implementations of
every rung are in `facilitator-solutions/`, and `measure.ps1` there will regenerate all of
it on yours in about twenty-five minutes. **Do that before you quote any of it**, because
the ratios move with cache sizes and runtime versions, not just the absolute laps.

Cumulative speedup against each language's own untouched starting point:

| # | Rung | C# | F# | Go | JavaScript |
|---|---|---|---|---|---|
| 0 | Baseline | 1.00x | 1.00x | 1.00x | 1.00x |
| 1 | Flat / typed array | **1.94x** | 1.00x | — | **2.29x** |
| 2 | Stop at sqrt(n) | **4.70x** | **2.19x** | **2.22x** | **6.02x** |
| 3 | Mark from `f*f` | 5.09x | 2.31x | 2.20x | 6.49x |
| 4 | Odds only | **11.56x** | **5.69x** | **5.15x** | **14.73x** |
| 5 | Bit packing | 10.99x | 6.36x | **6.70x** | 14.07x |
| 6 | Zero means prime | **11.89x** | **6.62x** | **6.82x** | 14.08x |

Baselines: C# 650 laps, F# 1245, Go 1344, JavaScript 422.

Three things to take from that table.

**The best result is not always rung 6.** In JavaScript it is rung 4, at 14.73x — the
bitset costs more than it saves there, and rung 6 does not win it back. A JavaScript pair
who stops at odds only has the best number available to them.

**The spread between languages is about the starting point, not the pair.** Go tops out at
6.82x and JavaScript at 14.73x for exactly the same six insights. Say this before anyone
compares.

**Two of the six rungs carry most of the win** — the square root bound and odds only,
between them 10.7x of C#'s 11.9x. Rungs 3, 5 and 6 are each within a few percent of
nothing, and rung 5 is negative in half the languages.

### Past the ladder

C# only, each measured independently against rung 6 rather than stacked on each other —
stacking a regression would contaminate everything above it.

| Variant | Against rung 6 | Cumulative | Verdict |
|---|---|---|---|
| 7 — cache blocking | **0.99x** | 11.77x | No win at n = 1,000,000. The data already fits |
| 8 — wheel, mod 30 | **4.01x** | **47.68x** | By far the biggest rung in the exercise |
| 9 — bounds check removal | **0.96x** | 11.47x | A regression |
| Pooled buffer *(off ladder)* | **1.01x** | 12.02x | Nothing. See the rules call |

### Rung 6 without the bitset

Rung 6 is described above as sitting on top of rung 5, but it does not have to. Inverting
the sense so zero means prime works just as well on rung 4's odds only **byte array**, and
deletes the fill without adding an instruction to any write.

The order you hand out nudges depends on this, so it was measured separately, in two full
replications on a later and busier day. Absolute lap rates that day ran about 25% below the
session above with worse noise, so these are **step ratios against a rung 4 measured
alongside them** — not cumulative figures comparable with the table above.

| Step over rung 4 | C# | F# | Go | JavaScript |
|---|---|---|---|---|
| Replication A | 1.04x | 1.29x | 1.11x | 1.02x |
| Replication B | 1.09x | 1.33x | 1.10x | 0.98x |

**It never costs anything.** The worst of the eight observations is 0.98x, inside that day's
noise. That is structural rather than lucky: inverting the sense only ever *removes* a pass
over the array, where the bitset trades extra instructions on every write for a smaller
footprint and can therefore lose. It is the one rung on the board with no downside.

**What it is worth varies enormously by language.** F# gains about 1.3x. C# gains a little.
JavaScript gains nothing measurable — which is fine, because it costs nothing either.

As a destination rather than a stepping stone, though, the picture changes:

| Byte array route against bitset route | C# | F# | Go | JavaScript |
|---|---|---|---|---|
| rung 6 on bytes / rung 6 on a bitset | 1.02x | 1.15x | **0.85x** | 1.04x |

**Go is the exception, and it is a big one.** Go rewards the bitset more than any other
language here (rung 5 alone is 1.30x), and stopping at the byte array leaves about 15% on the
table. F# is the opposite: it should stay on bytes. C# and JavaScript are indifferent.

So point every pair at rung 6 before rung 5, because it cannot hurt them — then tell a Go
pair in particular that the bitset is worth going on for.

### The replications, and why they are reassuring

Those two replications re-measured everything else as well, and the result is worth knowing
before you quote any number on this page.

Absolute rates moved by about a quarter between days. The **ratios barely moved at all**.
Rung 5's step over rung 4, measured three times on three different days:

| | C# | F# | Go | JavaScript |
|---|---|---|---|---|
| Main session | 0.950x | 1.118x | 1.302x | 0.955x |
| Replication A | 0.945x | 1.116x | 1.318x | 0.951x |
| Replication B | 0.967x | 1.143x | 1.292x | 0.915x |

Every finding on this page that is expressed as a ratio survived. That is exactly the case
for scoring each team against their own baseline rather than against raw laps — and it is
the same argument you are making to the room at the intro, so it is worth having the numbers
to hand when somebody asks whether any of this is reproducible.

The raw rows for both replications are in `facilitator-solutions/replications/`. The main
`results.csv` and `summary.csv` remain the evidence for the tables above, from the one
session that produced them.

### How it was measured

| | |
|---|---|
| Machine | Intel i9-12900HK, 6 P-cores + 8 E-cores, 48 KiB L1d and 1.25 MB L2 per P-core, 24 MB L3 |
| OS | Windows 11 Pro 10.0.26200 |
| Runtimes | .NET 10.0.401, Node v24.13.0, Go 1.27.0 |
| Repetitions | 5 per configuration, 31 configurations, shuffled, baselines inside the shuffle |
| Statistic | Maximum laps per second across the 5 |
| Affinity | Pinned to the 12 P-core logical CPUs |
| Date | September 2026 |

Four choices in there are worth knowing about, because they change the answers.

**Laps per second, not laps.** Every harness tests its `while` condition before each lap,
so a run overruns five seconds by up to one lap — 0.2% at the slowest baseline.

**Maximum, not median or mean.** Laps completed in a fixed window is one-sided noise:
interference removes laps, nothing adds them. The maximum is the best estimate of what the
machine can actually do. This is not academic — one repetition ran 2.3% slow across the
board, and in it the Go baseline dropped 25% and F# rung 3 dropped 22%. A median would have
carried part of that into the published numbers; the maximum discards it.

**Pinned to all twelve P-core threads, not to one core.** Excluding the E-cores matters — a
sweep of the same variant across all twenty logical CPUs measured 1940 laps/s on CPUs 0–11
and 950–980 on CPUs 12–19, so an unpinned race that lands on an E-core reads half speed.
But pinning to a *single* core is worse than not pinning at all: .NET's concurrent GC, Go's
concurrent mark and V8's scavenger all run on other threads, and crowding them onto the
sieve's own core costs in proportion to allocation per lap — which is exactly what each
rung reduces. That would have inflated the very rungs being measured.

**Shuffled, with the baselines shuffled in too.** Running 31 configurations in order over
twenty minutes on a laptop confounds rung number with how hot the machine has got.
Measured drift across the session was under 0.5% either side, apart from the one slow
repetition noted above.

### Numbers this page used to carry

Regenerating everything changed several figures that had been on this page for a while. If
you have taught from it before, these are the ones that moved:

| Was | Now | |
|---|---|---|
| Rungs 1–3 worth "roughly 2.2x" in C# | **5.09x** | The old figure came from a different session and a slower rung 2 |
| C# baseline 617 laps, and 610 elsewhere on the page | **650** | The page contradicted itself; one measured set now |
| Full C# bitset "12.28x" | **11.89x** | Same ballpark, re-measured |
| Bit packing "about twice as fast overall" | **0.95x–1.30x** | Negative in C# and JavaScript |
| The wheel is "a comparable win" to rung 6 | **4.01x** | The largest rung in the exercise |
| Bounds check removal "worth a few tens of percent" | **0.96x** | And pointers will not compile |
| Pooling is "a large win" | **1.01x** | Which makes the rules call much easier |

The per-operation tables from `cache-cliff` elsewhere on this page are **not** comparable
with any of these. They measure the cost of a single marking operation in nanoseconds with
a different tool; the numbers here are whole-sieve throughput ratios. Do not try to
reconcile them.

## The solutions

Every rung, in every language, is implemented in `facilitator-solutions/`. Each file is a
complete drop-in replacement for that race's sieve file with the same public API, so you can
copy one over the real thing and run the race unchanged:

```
cp facilitator-solutions/csharp/rung-6-zero-means-prime.cs csharp/Sieve.cs
cd csharp && dotnet run -c Release
```

Each carries a header explaining what the rung does, why it works and what breaks — written
to be read aloud in a debrief. The directory's own README covers the measurement method.

**They are verified beyond the self test.** The harness checks sizes 10 through 100,000,
but every size it tests is composite, so it cannot tell "primes below n" from "primes up to
n"; and none is odd, so it cannot catch an index count of `n/2` that should be `(n+1)/2`.
Both would pass here and then bite a team who copied the answer. Every solution was checked
against an independent trial-division reference at twenty sizes including 0, 1, 2, 3, 9, 11,
29, 31, 49, 101, 127, 999, 9,973 and 100,001.

### The end of the ladder

C#, rung 6 — the whole sieve is these fifteen lines:

```csharp
public Sieve(int sieveSize)
{
	_sieveSize = sieveSize;
	_count = (sieveSize + 1) / 2;              // odd candidates only
	_words = new ulong[(_count + 63) / 64];    // already zeroed, and zero means prime
}

public void Run()
{
	int limit = (int)Math.Sqrt(_sieveSize);

	for (int factor = 3; factor <= limit; factor += 2)
	{
		int f = factor >> 1;

		if ((_words[f >> 6] & (1UL << f)) == 0)
		{
			for (int q = (factor * factor) >> 1; q < _count; q += factor)
			{
				_words[q >> 6] |= 1UL << q;
			}
		}
	}
}
```

Go, the same thing, and note the shift mask that C# gets for free:

```go
for factor := 3; factor <= limit; factor += 2 {
	f := factor >> 1

	if sieve.words[f>>6]&(1<<(uint(f)&63)) == 0 {
		for q := (factor * factor) >> 1; q < count; q += factor {
			sieve.words[q>>6] |= 1 << (uint(q) & 63)   // &63 is NOT optional in Go
		}
	}
}
```

F# has to carry its own size, because a `uint64[]` of 7,813 words cannot tell you whether n
was 1,000,000 or 1,000,063:

```fsharp
type State = { Words: uint64[]; Size: int }

let create sieveSize =
    let count = (sieveSize + 1) / 2
    { Words = Array.zeroCreate ((count + 63) / 64); Size = sieveSize }
```

JavaScript's best result is **rung 4**, not rung 6 — the bitset costs more than it saves:

```js
const count = (sieveSize + 1) >> 1;
const sieve = new Uint8Array(count);
sieve.fill(1);
// ...
for (let factor = 3; factor <= limit; factor += 2) {
	if (sieve[factor >> 1]) {
		for (let q = (factor * factor) >> 1; q < count; q += factor) {
			sieve[q] = 0;
		}
	}
}
```

### Why the wheel wins

The mod 30 wheel's inner loop is the reason it is four times faster than rung 6, and it is
worth putting on a screen next to the bitset loop above. For a fixed prime and a fixed
residue class the bit position never changes, so the mask leaves the loop entirely:

```csharp
byte mask = (byte)(1 << BitIndex[product % 30]);   // loop invariant

while (byteIdx < byteCount)
{
	_data[byteIdx] |= mask;      // no shift, no recomputation
	byteIdx += p;
}
```

Against the bitset's `_words[q >> 6] |= 1UL << q`, which recomputes a variable shift on
every write. Fewer candidates *and* less work per candidate — the only rung on the board
where those two pull the same way.

## Nudges, in order

Hand these out one at a time. Stop nudging as soon as a pair says the thing in the "you are
done here" line — after that they are working, not stuck.

**Which nudge to open with.** The numbering below is the ladder's order, not the order you
will hand them out. If you mobbed rung 2 as suggested, nudge 1 is already spent, so:

| Where the pair is | Open with |
|---|---|
| Just out of the mob, most pairs | **Nudge 3** — rung 4 is the afternoon's target |
| Still on a growable list (C#) | "What does `List<bool>.Add` do when it runs out of room?" |
| Still on `new Array(n)` (JavaScript) | "How many bytes do you think each `true` in that array takes?" |
| Go and F# | Nudge 3. They start flat and their rung 1 is worth nothing |
| Reached rung 4 with time left | Nudge 5, then 4 — in that order, see below |
| Reached rung 4 with time left, **in Go** | Nudge 5, then 4, and mean it — Go is the one language that really wants the bitset |

**Nudges 4, 5 and 6 are optional, and 4 is the one to be careful with.** Rungs 5 and 7 can
measure *negative*, so those nudges point at something worth investigating rather than at a
guaranteed win.

Nudge 5 aims at rung 6, and rung 6 applied to the byte array is measured at 1.04x to 1.33x
with a worst case of 0.98x — it is the only rung that cannot cost a pair anything. So hand
nudge 5 out **before** nudge 4, which is a change from the order these are numbered in.
Then let the bitset be the follow-on question rather than the next step: it is worth 1.30x
in Go and negative in C# and JavaScript.

Hold each one until a pair has been stuck for about fifteen minutes. Each is a question, not
an instruction, and the follow up is only for when the question lands flat.

### 1. "How many times does your outer loop run? How many times does it need to?"

Aims at rung 2.

*You are done here when they say* something like "it runs a million times but there is
nothing left to mark after a thousand".

*If it lands flat:* "What is the smallest prime factor of a composite number under a
million? How big can it be?"

*Wrong turn to expect:* bounding at `n / 2` instead of `√n`. It is correct and much weaker.
Ask them why half, and let them find the square root themselves rather than telling them.

### 2. "For the factor 7, what is the smallest multiple you actually need to mark?"

Aims at rung 3.

*You are done here when they say* 49, and can say why 14, 21, 28, 35 and 42 do not need
marking.

*If it lands flat:* "Who marked 14? Had they finished before you got to 7?"

*Wrong turn to expect:* none really, this is the safest rung on the ladder. If they get
there quickly, push them at the `q += 2 * factor` half step described under rung 3, which
banks half of rung 4's win with none of its risk.

### 3. "How many of the numbers you are storing could ever be prime?"

Aims at rung 4, and this is the one that generates the most stuck time in the whole session.

*You are done here when they say* "half of them are even, and only 2 is prime".

*If it lands flat:* "So what are you storing the even ones for?"

*Wrong turn to expect:* the index mapping, guaranteed, specifically the step size. Do not
pre-warn them — let the self test catch it, then help them read it. When it fails at size 10,
get them to draw the five slots on paper as 1, 3, 5, 7, 9 and walk the marking by hand. Two
minutes with a pen beats twenty in the debugger, and the lesson sticks.

### 4. "How many bytes is your sieve? How much of that fits in L1?"

Aims at rung 5.

*You are done here when they say* roughly "a megabyte, and L1 is about 48K, so it does not
fit even slightly".

*If it lands flat:* send them to `cache-cliff/` and let the measurement argue instead of
you. For a .NET pair that is `cd cache-cliff && dotnet run -c Release`. For a JavaScript
pair it is `cd cache-cliff && node --expose-gc cache-cliff.mjs`, and they should look at
the memory table at the top before anything else — eight bytes per candidate tends to end
the discussion.

*What to expect:* they benchmark the bitset, find it slower, and abandon it. **They are
right.** In C# and JavaScript rung 5 measures as a regression, and this nudge can therefore
send a pair backwards. Aim it at rung 4 — odds only, which pays 2.3x everywhere — and treat
the bitset as the follow-on question "and does making it smaller *again* help?", whose
honest answer here is usually no. See rung 5.

### 5. "You allocate a fresh one every lap. Does it need to be fresh, or just clean?"

Aims at rung 6.

*You are done here when they say* "the runtime already zeroes it, so if zero meant prime I
would not need the fill at all".

*If it lands flat:* "What is already in a `bool[]` the moment you allocate it? Could that
value mean prime instead?"

*Wrong turn to expect:* they hoist the whole allocation into a static and the constructor
becomes free. That is the pooling question — have your answer ready before you ask this one.

### 6. "What is the smallest your data could possibly be?"

Aims at rungs 7 and 8, and is really a debrief question that you can hand out early to a
pair who are flying.

*You are done here when* they are talking about skipping multiples of 3 and 5 as well as 2,
or about working in blocks that fit in cache.

*Caution:* blocking measures 0.99x at a million — say so before they spend an hour on it.

The wheel is the opposite, and this page had it wrong for a long time: it measures **4.01x**
against rung 6, the biggest rung in the exercise. It is still half a day's work and nobody
will finish it in an afternoon, so it remains a debrief item rather than a nudge. But if a
strong pair asks whether it is worth it, the honest answer is now "yes, enormously, and you
will not finish today".

### When not to nudge

A pair arguing loudly about whether the bitset should be faster is not stuck. A pair who
have gone quiet and are both reading the same twenty lines is. The difference is usually
audible from across the room.

If they are properly stuck rather than just quiet, `cache-cliff/` is the escape hatch that
does not spoil anything — it shows them the wall without naming the rung.

## Failure modes to watch for

**Stopping early.** The most common one by far. A pair gets the array and the square root
bound, sees a good number, and thinks they are done — and because rung 2 is worth 2.2x to
6x on its own, that number really is good. Prevention is one sentence in the briefing:
there are nine rungs, and odds only roughly doubles whatever they have when they get there.

Quote the finishing figures for their language rather than a single headline, because they
are not close to each other: **C# 11.9x, F# 6.6x, Go 6.8x, JavaScript 14.7x.**

**The odds only off by one.** Everybody hits it. The self test catches it and names the size
that broke, so point them at that rather than at their code.

**Believing small numbers.** Run to run noise is about 5% on an idle machine. Anything under
1.1x is not a result. If a pair is chasing a 3% change, tell them to close everything else
and run it again — and see [the machines you are measuring on](#the-machines-you-are-measuring-on),
because the swing from a race landing on an efficiency core is far bigger than 5%.

**Grinding on a rung that does not pay.** New, and now the one I would watch for hardest
after the odds only off by one. A pair who spends an hour on the bitset in C# or JavaScript
finishes *slower than they started*, and nothing tells them: the self test stays green
because the code is perfectly correct, and the only signal is a lap count they may not have
checked since before they began.

The tell is a pair who have been quiet a long time on rung 5. The intervention is to ask
what their **number** did, not what their code does — and if they have not re-run since
starting, that is the lesson, not the bitset.

**The F# pair fighting the language**, as above.

**Moving work into the results step.** The rules cover it, the timed region does not include
`Result()`. If someone finds this, it is worth a public mention rather than a quiet word,
because it is a genuinely clever observation about how benchmarks get gamed.

## Rules calls you will probably have to make

**Pooling the array across laps.** The constructor is inside the timed loop, so a static or
pooled buffer that the constructor merely clears looks like it should be a large win.

**Measured: 1.01x against rung 6. It is worth nothing**, which makes this the easiest call
on the list — allow it, and it will not distort anybody's number. The reason is that by
rung 6 the allocation is 61 KiB on the gen0 path and the runtime hands it back already
zeroed; there is nothing left to save. Pooling only looks valuable while the array is still
a megabyte, and by then the team has better rungs available.

Require that the sieve is genuinely recomputed each lap either way. And if a pair pools
early, at rung 1 or 2, it *will* help them — which is worth letting them discover and then
measure again after rung 6, when it has quietly stopped mattering.

**Changing the sieve size.** No. It lives in the off limits file, and the validation data
depends on it.

**Threads, SIMD, intrinsics.** Already ruled out in the README. The reason worth giving is
that buying cores or vector width teaches nothing about the algorithm.

**AI assistance.** Claude or Cursor will one shot a near optimal bitset sieve if asked
plainly, which deletes the exercise. Whatever you decide, decide it out loud at the start.
"Ask it to explain, not to implement" is the version that keeps the learning.

## Debrief

Twenty minutes, each pair answering two questions: which rung gave you the most, and what
surprised you. This is where the teams who only reached rung 3 learn about rungs 5 and 7,
and it is the reason it does not matter much if some pairs get stuck.

The answer you are hoping somebody gives, unprompted: *we stopped trying to do less work and
started trying to touch less memory.*
