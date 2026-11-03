# Compiles Effects\<Shader>.fx to Effects\<Shader>.xnb with the same fxcompiler.exe ModdersToolkit uses.
# The include (Effects\PixelShared.fxh) is pasted into the shader first (the compiler cannot resolve it). The compiler is run on a
# file in Effects\ (that is where it is known to write the .xnb), so the original .fx is swapped for the flattened text for the
# duration of the compile and ALWAYS put back afterwards.
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\compile_shader.ps1 -Shader DarkFlameOrb
param(
    [Parameter(Mandatory = $true)][string]$Shader,
    [string]$Root = 'C:\Users\ismai\OneDrive\Documents\My Games\Terraria\tModLoader\ModSources\IsmailsWeaponReworks',
    [string]$Compiler = 'C:\Users\ismai\OneDrive\Documents\My Games\Terraria\tModLoader\Mods\Cache\fxcompiler.exe'
)
$ErrorActionPreference = 'Stop'
$Shader = [IO.Path]::GetFileNameWithoutExtension($Shader)
$effects = Join-Path $Root 'Effects'
$fx = Join-Path $effects "$Shader.fx"
$xnb = Join-Path $effects "$Shader.xnb"
if (-not (Test-Path -LiteralPath $fx)) { Write-Host "Missing $fx" -ForegroundColor Red; exit 1 }
if (-not (Test-Path -LiteralPath $Compiler)) { Write-Host "Missing $Compiler (open ModdersToolkit once so it extracts the compiler)" -ForegroundColor Red; exit 1 }

# 1. flatten: replace every #include "x" line with the text of that file (works with LF and CRLF files)
$originalBytes = [IO.File]::ReadAllBytes($fx)
$text = [IO.File]::ReadAllText($fx).TrimStart([char]0xFEFF)
$evaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    ([IO.File]::ReadAllText((Join-Path $effects $m.Groups[1].Value))).TrimStart([char]0xFEFF)
}
$flat = [regex]::Replace($text, '(?m)^[ \t]*#include[ \t]+"([^"]+)"[^\r\n]*\r?$', $evaluator)
if ($flat -match '(?m)^\s*#include') { Write-Host 'An #include is still in the flattened text - aborting.' -ForegroundColor Red; exit 1 }
Write-Host ("Flattened {0}: {1} -> {2} characters" -f $Shader, $text.Length, $flat.Length)

# 2. compile (the original is restored whatever happens)
$started = Get-Date
$code = $null
$log = ''
try {
    [IO.File]::WriteAllText($fx, $flat, (New-Object System.Text.UTF8Encoding($true)))
    $log = (& $Compiler $fx 2>&1 | Out-String)
    $code = $LASTEXITCODE
}
finally {
    [IO.File]::WriteAllBytes($fx, $originalBytes)
}
Write-Host '--- compiler output ---'
Write-Host $log
Write-Host "--- exit code: $code ---"

# 3. report where the .xnb is
if (Test-Path -LiteralPath $xnb) {
    $item = Get-Item -LiteralPath $xnb
    if ($item.LastWriteTime -ge $started.AddSeconds(-2)) {
        Write-Host "OK: $xnb ($($item.Length) bytes). Now do a full Build + Reload." -ForegroundColor Green
        exit 0
    }
}
Write-Host "No fresh $Shader.xnb in Effects. Looking elsewhere ..." -ForegroundColor Yellow
$places = @($Root, (Split-Path $Compiler), [Environment]::GetFolderPath('Desktop'), [Environment]::CurrentDirectory) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique
$found = @(Get-ChildItem -LiteralPath $places -Recurse -Filter "$Shader.xnb" -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -ge $started.AddSeconds(-2) })
if ($found.Count -gt 0) {
    $found | ForEach-Object { Write-Host "Found: $($_.FullName) ($($_.Length) bytes) - copy it to $xnb" -ForegroundColor Yellow }
} else {
    Write-Host 'The compiler wrote no .xnb anywhere: the shader most likely has a compile error. Paste the compiler output above into the chat.' -ForegroundColor Red
}
exit 1