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
| Intro and rules | Everyone runs their race once, untouched, to record `baseline.txt` |
| First rung together | Mob it. Get from the naive sieve to the square root bound and `f*f` as a group |
| Pairs | The long stretch. Circulate, nudge, resist fixing it for them |
| Debrief | Each pair says which rung gave them the most, and what surprised them |

Mobbing the first rung matters more than it sounds. It gets a 2x on the board inside
twenty minutes, gives everyone shared vocabulary, and removes the cold start that makes
people feel stupid. It is the single highest value thing on this page.

## The ladder

Ordered by payoff per unit of effort, at n = 1,000,000.

| # | Rung | What it buys |
|---|---|---|
| 0 | Baseline | — |
| 1 | Growable list to fixed array | Removes ~20 reallocations and the copying with them |
| 2 | Stop the outer loop at sqrt(n) | Removes a **full sequential read of the whole array**, 1M reads down to 1000 |
| 3 | Start marking at `f*f`, not `2f` | Removes genuinely redundant writes |
| 4 | Odds only | Halves operations *and* footprint. First rung with an index mapping, so the first one that breaks |
| 5 | Bit packing | 8x smaller again, 16x combined. Trades instructions for footprint |
| 6 | Stop refilling the array | Invert the sense so zero means prime, and zero initialisation does the work free |
| 7 | Cache blocking | Sieve one L1 sized block with every prime before moving on |
| 8 | Wheel factorization (mod 30, mod 210) | 8 residues per 30 integers instead of 15 |

Rungs 1, 2 and 3 together are about five lines of change and are worth roughly **2.2x**
in C#, measured, 610 laps to 1364. They are the mob session.

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

**Why it works.** Not by doing less work — by touching less memory. See the counterintuitive
note below.

**This one is counterintuitive and teams will get it backwards.** Measured in C# at
n = 1,000,000, the bitset is *slower per operation* than the byte array, 0.825 ns against
0.626 ns, because a bit write is a read, a shift, an or and a write where a byte write is a
single store. It is still about twice as fast overall, because it does half the operations
in a sixteenth of the space. A pair who benchmarks per operation cost and concludes the
bitset is a bad idea has measured correctly and reasoned wrongly. That is a good
conversation to have rather than head off.

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

### Rung 7 — cache blocking

Every language, and usually not worth it here.

Instead of striding each prime across the whole array in turn, walk the array in blocks
sized to fit L1, and within each block mark the multiples of every prime before moving on.
Each prime needs to remember where it got to.

**Why it pays less here than people expect.** Once a pair is on an odds only bitset the whole
working set is 61 KiB and already sits comfortably in L2, so there is not much left to win.
Blocking is the right answer at a hundred million, not at a million. Say this out loud
before somebody spends an hour on it.

### Rung 8 — wheel factorization

The residues coprime to 30 are 1, 7, 11, 13, 17, 19, 23 and 29 — eight numbers in every
thirty, against fifteen in thirty for odds only. That is another 1.9x reduction in space, and
mod 210 takes it to 48 in 210.

The complexity goes up sharply: marking patterns differ per residue class, and the index
arithmetic stops being a single shift. Realistically this is a rung to *mention* in the
debrief rather than one anybody reaches in an afternoon. If a pair is flying and wants it,
let them, but make sure they have rung 6 first — it is a fraction of the effort for a
comparable win.

### Rung 9 — the micro stuff

Only worth it after everything above. Bounds check removal in C# via `Unsafe.Add` or
pointers, since the `q += factor` stride is not a shape the JIT can prove safe. Unrolling
the inner loop over the eight bit positions a given prime touches within a byte, which repeat
on a fixed cycle. Both are real, both are worth a few tens of percent, and both are a poor
use of the session compared with rungs 4 to 6.

## Where the languages stop sharing a ladder

Baselines measured on one machine, back to back:

| Language | Baseline laps | Starts with | Rung 1 applies? |
|---|---|---|---|
| Go | 1224 | `[]bool` | No, already there |
| F# | 1203 | `bool[]` | No, already there |
| C# | 617 | `List<bool>` | **Yes** |
| JavaScript | 395 | `new Array(n)` | **Yes, and it is the biggest rung in the whole exercise** |

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

Expect the largest headline number in the room. A full odds only bitset measured **12.28x**
against the C# baseline.

### F#

`Array.init sieveSize (fun _ -> true)` is **one million closure invocations**.
`Array.create sieveSize true` does a vectorised fill instead. F# only rung, nearly free, and
it is sitting right there in `Sieve.create`.

The real F# problem is cultural, not technical. The fastest F# sieve is a mutable array in a
`for` loop and looks like C# in a false moustache. A pair reaching for idiomatic immutable
F#, folds, `Seq`, list comprehensions, will go backwards and feel bad about it. Say this out
loud at the start.

### Go

Starts strongest and has the shortest ladder, so it will post the **smallest multiplier**.
Make sure the team knows that is the starting point's fault, not theirs.

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
and worth about 2.7x per operation on its own. The bitset, which is the headline rung in C#,
adds comparatively little at n = 1,000,000 because `Uint8Array` is already fast and already
small enough.

One more divergence: **JavaScript bitwise operators are 32 bit**, so a bitset wants
`Uint32Array` with `>>> 5` and `& 31`, not the 64 bit word layout the .NET versions use.
`BigUint64Array` involves BigInt and is slower, not faster.

## Expected multipliers

Rough, from one machine, and the point is the ordering rather than the numbers:

| Language | Plausible range | Why |
|---|---|---|
| JavaScript | Largest | Worst starting point, two free rungs before the interesting ones |
| C# | ~12x measured | `List<bool>` plus the LOH cliff |
| F# | Middle | Already on `bool[]`, but `Array.init` is a gift |
| Go | Smallest | Best starting point, shortest ladder |

**Tell the teams this.** Scoring against your own baseline is fair across machines but not
across languages, and a Go pair doing excellent work may post 4x while a C# pair posts 12x
for exactly the same insight.

## Nudges, in order

Hold these and hand out one at a time when a pair has been stuck for about fifteen minutes.
Each one is a question, not an instruction, and the follow up is only for when the question
lands flat. Stop nudging as soon as they say the thing in the "you are done here" line —
after that they are working, not stuck.

**Before nudge 1, check which language they are in.** The first rung is not the same one.

| Language | Open with |
|---|---|
| C# | "What does `List<bool>.Add` do when it runs out of room?" |
| JavaScript | "How many bytes do you think each `true` in that array takes?" |
| Go, F# | Skip to nudge 1, they already start on a flat array |

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

*Wrong turn to expect:* they benchmark the bitset, find it is slower per operation, and
abandon it. They have measured correctly — see rung 5. Ask what they are dividing by.

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

*Caution:* this is the point to say that blocking will not pay much at a million, and that
the wheel is a large amount of work for a modest win. Otherwise you have just sent your
strongest pair down the longest road on the board.

### When not to nudge

A pair arguing loudly about whether the bitset should be faster is not stuck. A pair who
have gone quiet and are both reading the same twenty lines is. The difference is usually
audible from across the room.

If they are properly stuck rather than just quiet, `cache-cliff/` is the escape hatch that
does not spoil anything — it shows them the wall without naming the rung.

## Failure modes to watch for

**Stopping at 2x.** The most common one by far. A pair gets the array and the square root
bound, sees a good number, and thinks they are done. Prevention is one sentence in the
briefing: there are at least eight rungs and 10x is normal.

**The odds only off by one.** Everybody hits it. The self test catches it and names the size
that broke, so point them at that rather than at their code.

**Believing small numbers.** Run to run noise is about 5% on an idle machine. Anything under
1.1x is not a result. If a pair is chasing a 3% change, tell them to close everything else
and run it again.

**The F# pair fighting the language**, as above.

**Moving work into the results step.** The rules cover it, the timed region does not include
`Result()`. If someone finds this, it is worth a public mention rather than a quiet word,
because it is a genuinely clever observation about how benchmarks get gamed.

## Rules calls you will probably have to make

**Pooling the array across laps.** The constructor is inside the timed loop, so a static or
pooled buffer that the constructor merely clears is a large win. My suggestion is allow it,
because "do not allocate in a hot loop" is a real and valuable lesson, but require that the
sieve is genuinely recomputed each lap. Decide before someone asks.

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
