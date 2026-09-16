using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

// The same measurement as cache-cliff.ipynb, without needing VS Code or any
// extension. Run it with: dotnet run -c Release

Console.WriteLine("Cache cliff - what one sieve operation costs as the working set grows");
Console.WriteLine();

foreach (string line in CacheInfo.Describe())
{
	Console.WriteLine(line);
}

Console.WriteLine();

// Half octave steps from 8 Ki up to 64 Mi, plus the race size itself.
List<int> sizes = [];
for (int k = 0; k <= 26; k++)
{
	sizes.Add((int)Math.Round(Math.Pow(2, 13 + (k / 2.0))));
}
sizes.Add(1_000_000);
sizes = sizes.Distinct().Order().ToList();

Console.WriteLine($"Measuring {sizes.Count} sizes. This takes a minute or two.");
Console.WriteLine();

// Warm everything up once at a middling size before any measurement. The per size
// warmup below is not enough on its own: the very first timed run in the process
// pays for tiered JIT, and that lands on the smallest size and quietly ruins it.
{
	int warmN = 1 << 20;
	bool[] warmSieve = new bool[warmN + 1];
	ulong[] warmWords = new ulong[(((warmN + 1) / 2) + 63) / 64];

	for (int i = 0; i < 5; i++)
	{
		Array.Fill(warmSieve, true);
		Sieves.SieveBytes(warmSieve, warmN);
		Array.Clear(warmWords);
		Sieves.SieveBits(warmWords, warmN);
	}
}

List<Sample> samples = [];

foreach (int n in sizes)
{
	bool[] sieve = new bool[n + 1];
	ulong[] words = new ulong[(((n + 1) / 2) + 63) / 64];

	Array.Fill(sieve, true);
	long opsBytes = Sieves.CountBytes(sieve, n);
	Array.Clear(words);
	long opsBits = Sieves.CountBits(words, n);

	double msBytes = Measure(() => Array.Fill(sieve, true), () => Sieves.SieveBytes(sieve, n));
	double msBits = Measure(() => Array.Clear(words), () => Sieves.SieveBits(words, n));

	samples.Add(new Sample(
		n,
		n + 1,
		words.Length * 8,
		msBytes * 1e6 / opsBytes,
		msBits * 1e6 / opsBits,
		msBytes,
		msBits));

	if (!Console.IsOutputRedirected)
	{
		Console.Write($"\r  {samples.Count} of {sizes.Count} done");
	}

	GC.Collect();
	GC.WaitForPendingFinalizers();
}

Console.WriteLine();
Console.WriteLine();

// ---- the numbers ------------------------------------------------------------

Console.WriteLine($"{"n",13}  {"byte[]",10}  {"bitset",10}  {"byte[] ns/op",13}  {"bitset ns/op",13}");
Console.WriteLine($"{new string('-', 13)}  {new string('-', 10)}  {new string('-', 10)}  {new string('-', 13)}  {new string('-', 13)}");

foreach (Sample s in samples)
{
	Console.WriteLine(
		$"{s.N,13:N0}  {Format.Bytes(s.ArrayBytes),10}  {Format.Bytes(s.BitsetBytes),10}  {s.NsPerOpArray,13:F3}  {s.NsPerOpBitset,13:F3}");
}

// ---- the picture ------------------------------------------------------------

double scale = Math.Max(samples.Max(s => s.NsPerOpArray), samples.Max(s => s.NsPerOpBitset));

Chart("Cost per marking operation, one byte per candidate", s => s.NsPerOpArray);
Chart("Cost per marking operation, one bit per odd candidate (same scale)", s => s.NsPerOpBitset);

// ---- the point --------------------------------------------------------------

Sample race = samples.First(s => s.N == 1_000_000);

Console.WriteLine("At the race size, n = 1,000,000:");
Console.WriteLine($"  one byte per candidate    {Format.Bytes(race.ArrayBytes),10}  {race.NsPerOpArray,7:F3} ns/op  {race.MsArray,8:F3} ms per sieve");
Console.WriteLine($"  one bit per odd candidate {Format.Bytes(race.BitsetBytes),10}  {race.NsPerOpBitset,7:F3} ns/op  {race.MsBitset,8:F3} ms per sieve");
Console.WriteLine();

Console.WriteLine("Across every size measured:");
Range("one byte per candidate   ", s => s.NsPerOpArray);
Range("one bit per odd candidate", s => s.NsPerOpBitset);

Console.WriteLine();
Console.WriteLine("The byte array does a single store per operation and the bitset does a read,");
Console.WriteLine("a shift, an or and a write, so the bitset starts out slower. It is sixteen");
Console.WriteLine("times smaller, so it stays in a given cache level sixteen times longer. That");
Console.WriteLine("is the whole trade: more instructions, less memory.");
Console.WriteLine();
Console.WriteLine("This times the marking loop only. Allocating and refilling the sieve each lap");
Console.WriteLine("is excluded here but is real cost in the race.");

// ---- csv for anyone who wants to plot it properly ---------------------------

StringBuilder csv = new();
csv.AppendLine("n,arrayBytes,bitsetBytes,nsPerOpArray,nsPerOpBitset,msArray,msBitset");
foreach (Sample s in samples)
{
	csv.AppendLine(string.Join(',', new[]
	{
		s.N.ToString(CultureInfo.InvariantCulture),
		s.ArrayBytes.ToString(CultureInfo.InvariantCulture),
		s.BitsetBytes.ToString(CultureInfo.InvariantCulture),
		s.NsPerOpArray.ToString("F4", CultureInfo.InvariantCulture),
		s.NsPerOpBitset.ToString("F4", CultureInfo.InvariantCulture),
		s.MsArray.ToString("F4", CultureInfo.InvariantCulture),
		s.MsBitset.ToString("F4", CultureInfo.InvariantCulture),
	}));
}

File.WriteAllText("cache-cliff.csv", csv.ToString());
Console.WriteLine();
Console.WriteLine("Wrote cache-cliff.csv");

return 0;

// ---- helpers ----------------------------------------------------------------

void Chart(string title, Func<Sample, double> pick)
{
	Console.WriteLine();
	Console.WriteLine(title);
	Console.WriteLine();

	foreach (Sample s in samples)
	{
		double value = pick(s);
		int filled = (int)Math.Round(48 * value / scale);
		string bar = new('#', Math.Clamp(filled, 1, 48));
		Console.WriteLine($"{s.N,13:N0}  {bar,-48}  {value,6:F3}");
	}

	Console.WriteLine();
}

void Range(string label, Func<Sample, double> pick)
{
	// Best against the largest size, rather than min against max. The largest size
	// is the one that is actually out of cache, so that is the comparison that means
	// something; a stray high reading somewhere in the middle is not.
	double best = samples.Min(pick);
	Sample biggest = samples[^1];
	double atBiggest = pick(biggest);

	Console.WriteLine(
		$"  {label}  best {best,6:F3}, and {atBiggest,6:F3} at n = {biggest.N:N0}  ({atBiggest / best,4:F1}x)");
}

double Measure(Action reset, Action run)
{
	// Tiered JIT needs several calls before it promotes a method. Without this the
	// small sizes read far too slow and flatten the effect we are trying to show.
	Stopwatch warm = Stopwatch.StartNew();
	int warmReps = 0;
	while (warm.ElapsedMilliseconds < 100 && warmReps < 30)
	{
		reset();
		run();
		warmReps++;
	}

	Stopwatch sw = new();
	double totalMs = 0;
	int reps = 0;

	while (totalMs < 150 && reps < 2000)
	{
		reset();
		sw.Restart();
		run();
		sw.Stop();
		totalMs += sw.Elapsed.TotalMilliseconds;
		reps++;
	}

	return totalMs / reps;
}

readonly record struct Sample(
	int N,
	long ArrayBytes,
	long BitsetBytes,
	double NsPerOpArray,
	double NsPerOpBitset,
	double MsArray,
	double MsBitset);

static class Format
{
	public static string Bytes(long bytes)
	{
		if (bytes >= 1024 * 1024)
		{
			return $"{Round(bytes / (1024.0 * 1024.0))} MiB";
		}

		if (bytes >= 1024)
		{
			return $"{Round(bytes / 1024.0)} KiB";
		}

		return $"{bytes} B";
	}

	// A 1.25 MiB L2 reported as "1 MiB" is worse than useless when the whole point
	// is comparing a footprint against a cache size, so keep a decimal when it matters.
	private static string Round(double value)
	{
		return Math.Abs(value - Math.Round(value)) < 0.05
			? value.ToString("F0", CultureInfo.InvariantCulture)
			: value.ToString("F1", CultureInfo.InvariantCulture);
	}
}

static class Sieves
{
	// One byte per candidate. Square root bound, start marking at f*f.
	public static void SieveBytes(bool[] sieve, int n)
	{
		int limit = (int)Math.Sqrt(n);
		for (int f = 2; f <= limit; f++)
		{
			if (sieve[f])
			{
				for (int q = f * f; q <= n; q += f)
				{
					sieve[q] = false;
				}
			}
		}
	}

	// The identical loop, counting the writes instead of only doing them.
	public static long CountBytes(bool[] sieve, int n)
	{
		long ops = 0;
		int limit = (int)Math.Sqrt(n);
		for (int f = 2; f <= limit; f++)
		{
			if (sieve[f])
			{
				for (int q = f * f; q <= n; q += f)
				{
					sieve[q] = false;
					ops++;
				}
			}
		}

		return ops;
	}

	// One bit per odd candidate. Bit i represents the number 2i+1.
	public static void SieveBits(ulong[] words, int n)
	{
		int bits = (n + 1) / 2;
		int limit = (int)Math.Sqrt(n);
		for (int f = 3; f <= limit; f += 2)
		{
			int fi = f >> 1;
			if ((words[fi >> 6] & (1UL << fi)) != 0)
			{
				continue;
			}

			for (int q = (f * f) >> 1; q < bits; q += f)
			{
				words[q >> 6] |= 1UL << q;
			}
		}
	}

	public static long CountBits(ulong[] words, int n)
	{
		long ops = 0;
		int bits = (n + 1) / 2;
		int limit = (int)Math.Sqrt(n);
		for (int f = 3; f <= limit; f += 2)
		{
			int fi = f >> 1;
			if ((words[fi >> 6] & (1UL << fi)) != 0)
			{
				continue;
			}

			for (int q = (f * f) >> 1; q < bits; q += f)
			{
				words[q >> 6] |= 1UL << q;
				ops++;
			}
		}

		return ops;
	}
}

// Per cache sizes straight from the OS. Unlike the WMI query the notebook uses,
// GetLogicalProcessorInformationEx reports each physical cache separately, so on a
// hybrid CPU you see the real per core figures rather than a per cluster total.
static class CacheInfo
{
	private const int RelationCache = 2;
	private const int ErrorInsufficientBuffer = 122;

	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetLogicalProcessorInformationEx(
		int relationshipType, byte[]? buffer, ref uint returnedLength);

	public static IEnumerable<string> Describe()
	{
		SortedDictionary<int, SortedSet<uint>>? byLevel;

		try
		{
			byLevel = Read();
		}
		catch (Exception)
		{
			byLevel = null;
		}

		if (byLevel is null)
		{
			yield return "Could not read cache sizes on this platform.";
			yield return "The measurement below does not depend on them.";
			yield break;
		}

		if (byLevel.Count == 0)
		{
			yield return "No cache information reported by the OS.";
			yield break;
		}

		yield return "Data and unified caches reported by the OS, per cache:";

		foreach ((int level, SortedSet<uint> cacheSizes) in byLevel)
		{
			string sizes = string.Join(", ", cacheSizes.Select(b => Format.Bytes(b)));
			yield return $"  L{level}  {sizes}";
		}

		yield return "";
		yield return "Where several sizes appear at one level the CPU has more than one kind of";
		yield return "core. A single threaded sieve only ever sees one of them at a time.";
	}

	private static SortedDictionary<int, SortedSet<uint>> Read()
	{
		SortedDictionary<int, SortedSet<uint>> byLevel = [];

		if (!OperatingSystem.IsWindows())
		{
			return byLevel;
		}

		uint length = 0;
		if (GetLogicalProcessorInformationEx(RelationCache, null, ref length))
		{
			return byLevel;
		}

		if (Marshal.GetLastWin32Error() != ErrorInsufficientBuffer)
		{
			return byLevel;
		}

		byte[] buffer = new byte[length];
		if (!GetLogicalProcessorInformationEx(RelationCache, buffer, ref length))
		{
			return byLevel;
		}

		int offset = 0;
		while (offset + 8 <= length)
		{
			int relationship = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset, 4));
			int size = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(offset + 4, 4));

			if (size <= 0 || offset + size > length)
			{
				break;
			}

			if (relationship == RelationCache && offset + 20 <= length)
			{
				byte level = buffer[offset + 8];
				uint cacheSize = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 12, 4));
				uint cacheType = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset + 16, 4));

				// 0 = unified, 1 = instruction, 2 = data, 3 = trace. The sieve only
				// cares about where its data lives, so instruction caches are noise.
				if (cacheType is 0 or 2 && cacheSize > 0)
				{
					if (!byLevel.TryGetValue(level, out SortedSet<uint>? set))
					{
						set = [];
						byLevel[level] = set;
					}

					set.Add(cacheSize);
				}
			}

			offset += size;
		}

		return byLevel;
	}
}
