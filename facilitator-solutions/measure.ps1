<#
.SYNOPSIS
    Measures every rung of the ladder in every language and writes results.csv.

.DESCRIPTION
    Two phases. Phase 1 stages a scratch copy of the four races and builds every
    variant into its own output directory. Phase 2 races the pre-built binaries,
    shuffled and repetition-major, and parses laps from stdout.

    Nothing is built during measurement, and the repository working tree is never
    modified - the whole run happens in a scratch directory.

.NOTES
    Why the design is the way it is:

    * Variants are pre-built, not copy-built-run in the loop. Copy-Item preserves
      LastWriteTime, so a variant checked out of git can be OLDER than the previous
      rung's Race.dll, MSBuild's incremental check skips the compile, and you
      silently race the wrong binary. Hence --no-incremental and a timestamp stamp.

    * Affinity is a P-core MASK, not a single core. Every one of these runtimes does
      memory management on other threads (concurrent GC, V8's scavenger). Pinning to
      one core piles that onto the sieve's own core, and the cost is proportional to
      allocation per lap - exactly what each rung reduces. That would inflate the
      very rungs we are trying to measure.

    * Runs are shuffled with the baselines inside the shuffle, so thermal drift
      shows up as noise in both numerator and denominator instead of masquerading
      as a rung effect.

    * The headline statistic is the MAXIMUM rate over repetitions. Lap count in a
      fixed window is one-sided noise: interference removes laps, nothing adds
      them. The max is the best estimate of the machine's real capability; a median
      is dragged down by whatever happened to be running.

    * Rate is laps/elapsed, not laps/5. The harness tests its while condition before
      each lap, so every run overruns 5s by up to one lap.
#>

[CmdletBinding()]
param(
    [int]$Reps = 5,
    [string]$Bench = (Join-Path $env:TEMP 'sieve-bench'),
    [switch]$SkipSweep
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$solutions = $PSScriptRoot

Write-Host "Repo:      $repo"
Write-Host "Scratch:   $Bench"

# ---------------------------------------------------------------- guard ----

$dirty = git -C $repo status --porcelain -- csharp/Sieve.cs fsharp/Sieve.fs go/primeSieve.go javascript/primeSieve.js
if ($dirty) {
    throw "The four sieve files have uncommitted changes. Commit or stash first.`n$dirty"
}

# ------------------------------------------------------- phase 1: stage ----

if (Test-Path $Bench) { Remove-Item $Bench -Recurse -Force }
New-Item -ItemType Directory -Path $Bench | Out-Null

foreach ($lang in 'csharp', 'fsharp', 'go', 'javascript') {
    $dest = Join-Path $Bench "src\$lang"
    New-Item -ItemType Directory -Path $dest -Force | Out-Null
    Get-ChildItem (Join-Path $repo $lang) -Force |
        Where-Object { $_.Name -notin 'bin', 'obj', '.idea' } |
        Copy-Item -Destination $dest -Recurse -Force
}

$configs = [System.Collections.Generic.List[object]]::new()

function Add-Config($lang, $variant, $workDir, $kind, $exe, $sourceFile) {
    $configs.Add([pscustomobject]@{
        Language = $lang
        Variant  = $variant
        WorkDir  = $workDir
        Kind     = $kind
        Exe      = $exe
        Sha256   = if ($sourceFile) { (Get-FileHash $sourceFile -Algorithm SHA256).Hash } else { '' }
    })
}

# The pristine sieve of each race is that race's rung 0.
$baselineSources = @{
    csharp     = Join-Path $repo 'csharp\Sieve.cs'
    fsharp     = Join-Path $repo 'fsharp\Sieve.fs'
    go         = Join-Path $repo 'go\primeSieve.go'
    javascript = Join-Path $repo 'javascript\primeSieve.js'
}

$sieveFileName = @{
    csharp     = 'Sieve.cs'
    fsharp     = 'Sieve.fs'
    go         = 'primeSieve.go'
    javascript = 'primeSieve.js'
}

function Build-DotNet($lang, $variant, $sourceFile) {
    $src = Join-Path $Bench "src\$lang"
    $target = Join-Path $src $sieveFileName[$lang]
    Copy-Item $sourceFile $target -Force
    (Get-Item $target).LastWriteTime = Get-Date

    $projectFile = if ($lang -eq 'csharp') { 'Race.csproj' } else { 'Race.fsproj' }
    $project = Join-Path $src $projectFile
    $out = Join-Path $Bench "build\$lang\$variant"

    dotnet build $project -c Release -o $out --no-incremental 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "$lang/$variant failed to build" }

    Add-Config $lang $variant $out 'exe' (Join-Path $out 'Race.exe') $sourceFile
}

function Build-Go($variant, $sourceFile) {
    $src = Join-Path $Bench 'src\go'
    Copy-Item $sourceFile (Join-Path $src 'primeSieve.go') -Force
    $out = Join-Path $Bench "build\go\$variant"
    New-Item -ItemType Directory -Path $out -Force | Out-Null

    Push-Location $src
    try {
        go build -o (Join-Path $out 'race.exe') .
        if ($LASTEXITCODE -ne 0) { throw "go/$variant failed to build" }
    } finally { Pop-Location }

    Add-Config 'go' $variant $out 'exe' (Join-Path $out 'race.exe') $sourceFile
}

function Build-Node($variant, $sourceFile) {
    $src = Join-Path $Bench 'src\javascript'
    $out = Join-Path $Bench "build\javascript\$variant"
    New-Item -ItemType Directory -Path $out -Force | Out-Null
    Copy-Item (Join-Path $src 'index.js') $out -Force
    Copy-Item (Join-Path $src 'package.json') $out -Force
    Copy-Item $sourceFile (Join-Path $out 'primeSieve.js') -Force

    Add-Config 'javascript' $variant $out 'node' 'node' $sourceFile
}

Write-Host "`nPhase 1: building variants..."

foreach ($lang in 'csharp', 'fsharp') {
    Build-DotNet $lang 'rung-0-baseline' $baselineSources[$lang]
    foreach ($f in Get-ChildItem (Join-Path $solutions $lang) -Filter "*.$(if($lang -eq 'csharp'){'cs'}else{'fs'})" | Sort-Object Name) {
        Build-DotNet $lang $f.BaseName $f.FullName
        Write-Host "  built $lang/$($f.BaseName)"
    }
}

Build-Go 'rung-0-baseline' $baselineSources['go']
foreach ($f in Get-ChildItem (Join-Path $solutions 'go') -Filter '*.go' | Sort-Object Name) {
    Build-Go $f.BaseName $f.FullName
    Write-Host "  built go/$($f.BaseName)"
}

Build-Node 'rung-0-baseline' $baselineSources['javascript']
foreach ($f in Get-ChildItem (Join-Path $solutions 'javascript') -Filter '*.js' | Sort-Object Name) {
    Build-Node $f.BaseName $f.FullName
    Write-Host "  staged javascript/$($f.BaseName)"
}

Write-Host "$($configs.Count) configurations."

# ------------------------------------------------------------- racing ----

function Invoke-Race($cfg, $AffinityMask) {
    # The process we launch is the process we measure - everything is pre-built, so
    # neither `dotnet run` nor `go run` is in the picture and there is no forked
    # child to lose the affinity mask to. In the main run the mask is inherited from
    # this host process; $AffinityMask is only used by the pre-flight core sweep.
    $stdout = [System.IO.Path]::GetTempFileName()
    Remove-Item (Join-Path $cfg.WorkDir 'baseline.txt') -ErrorAction SilentlyContinue

    try {
        $start = @{
            FilePath               = if ($cfg.Kind -eq 'node') { 'node' } else { $cfg.Exe }
            WorkingDirectory       = $cfg.WorkDir
            NoNewWindow            = $true
            PassThru               = $true
            RedirectStandardOutput = $stdout
        }
        if ($cfg.Kind -eq 'node') { $start.ArgumentList = @('.') }

        $process = Start-Process @start
        if ($AffinityMask) { $process.ProcessorAffinity = $AffinityMask }
        $process.WaitForExit()

        $text = Get-Content $stdout -Raw
    } finally {
        Remove-Item $stdout -ErrorAction SilentlyContinue
    }

    if ($text -notmatch 'Laps:\s*(\d+)\s+Time:\s*([\d.,]+)\s+#Primes:\s*(\d+)\s+Valid:\s*(\w+)') {
        throw "Could not parse output of $($cfg.Language)/$($cfg.Variant):`n$text"
    }

    $laps = [int]$Matches[1]
    $seconds = [double]::Parse($Matches[2].Replace(',', '.'), [Globalization.CultureInfo]::InvariantCulture)
    $primes = [int]$Matches[3]
    $valid = $Matches[4] -ieq 'true'

    if (-not $valid -or $primes -ne 78498) {
        throw "$($cfg.Language)/$($cfg.Variant) produced an invalid result: $primes primes, Valid=$valid"
    }

    [pscustomobject]@{
        Laps = $laps; Seconds = $seconds; Rate = $laps / $seconds
    }
}

# ------------------------------------------- pre-flight: which cores are P ----

$cpu = Get-CimInstance Win32_Processor
$logical = $cpu.NumberOfLogicalProcessors
$physical = $cpu.NumberOfCores
$pCores = $logical - $physical          # hybrid: 20 - 14 = 6 P-cores
$pLogical = 2 * $pCores                 # 12 SMT threads on the P-cores
$mask = [IntPtr]((1L -shl $pLogical) - 1)

Write-Host "`nCPU: $($cpu.Name)"
Write-Host "$logical logical / $physical cores => $pCores P-cores, mask 0x$(('{0:X}' -f $mask.ToInt64()))"

if (-not $SkipSweep) {
    Write-Host "`nPre-flight: racing one variant on each logical CPU to find the E-cores..."
    $probe = $configs | Where-Object { $_.Language -eq 'go' -and $_.Variant -like 'rung-6*' } | Select-Object -First 1
    $sweep = foreach ($id in 0..($logical - 1)) {
        $r = Invoke-Race $probe ([IntPtr](1L -shl $id))
        [pscustomobject]@{ Cpu = $id; Rate = [math]::Round($r.Rate, 1) }
    }
    $sweep | Format-Table -AutoSize | Out-String | Write-Host
    $sweep | Export-Csv (Join-Path $solutions 'core-sweep.csv') -NoTypeInformation
}

# ------------------------------------------------- phase 2: measurement ----

[System.Diagnostics.Process]::GetCurrentProcess().ProcessorAffinity = $mask
$env:GOMAXPROCS = $pLogical

Write-Host "`nPhase 2: $Reps repetitions x $($configs.Count) configurations..."

$results = [System.Collections.Generic.List[object]]::new()

foreach ($rep in 1..$Reps) {
    $order = $configs | Sort-Object { Get-Random }
    $i = 0
    foreach ($cfg in $order) {
        $i++
        Write-Progress -Activity "Rep $rep/$Reps" -Status "$($cfg.Language)/$($cfg.Variant)" -PercentComplete (100 * $i / $configs.Count)
        $r = Invoke-Race $cfg
        $results.Add([pscustomobject]@{
            Rep       = $rep
            At        = [DateTime]::UtcNow.ToString('o')
            Language  = $cfg.Language
            Variant   = $cfg.Variant
            Laps      = $r.Laps
            Seconds   = [math]::Round($r.Seconds, 6)
            Rate      = [math]::Round($r.Rate, 3)
            Sha256    = $cfg.Sha256.Substring(0, 12)
        })
        Start-Sleep -Milliseconds 2000
    }
    Write-Host "  rep $rep done"
}

$results | Export-Csv (Join-Path $solutions 'results.csv') -NoTypeInformation
Write-Host "`nWrote results.csv ($($results.Count) rows)."

# ---------------------------------------------------------- aggregate ----

$summary = foreach ($group in $results | Group-Object Language, Variant) {
    $rates = $group.Group.Rate
    $sorted = $rates | Sort-Object
    [pscustomobject]@{
        Language = $group.Group[0].Language
        Variant  = $group.Group[0].Variant
        MaxRate  = [math]::Round(($rates | Measure-Object -Maximum).Maximum, 2)
        MedRate  = [math]::Round($sorted[[int]($sorted.Count / 2)], 2)
        MinRate  = [math]::Round(($rates | Measure-Object -Minimum).Minimum, 2)
        Spread   = [math]::Round(($rates | Measure-Object -Maximum).Maximum / ($rates | Measure-Object -Minimum).Minimum, 3)
    }
}

$baselineRate = @{}
foreach ($s in $summary | Where-Object Variant -eq 'rung-0-baseline') { $baselineRate[$s.Language] = $s.MaxRate }

$summary = $summary | ForEach-Object {
    $_ | Add-Member -NotePropertyName Speedup -NotePropertyValue ([math]::Round($_.MaxRate / $baselineRate[$_.Language], 2)) -PassThru
} | Sort-Object Language, Variant

$summary | Export-Csv (Join-Path $solutions 'summary.csv') -NoTypeInformation
$summary | Format-Table -AutoSize | Out-String -Width 200 | Write-Host

# Drift: how much did each language's baseline move across the session?
Write-Host "Baseline drift across repetitions:"
$results | Where-Object Variant -eq 'rung-0-baseline' | Group-Object Language | ForEach-Object {
    $r = $_.Group.Rate
    $lo = ($r | Measure-Object -Minimum).Minimum
    $hi = ($r | Measure-Object -Maximum).Maximum
    Write-Host ("  {0,-11} {1,8:N1} - {2,8:N1} laps/s  spread {3:N3}x" -f $_.Name, $lo, $hi, ($hi / $lo))
}
