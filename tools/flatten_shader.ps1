# Inlines the #include "..." lines of an Effects\*.fx into one standalone file, for compilers that cannot resolve includes
# (for example ModdersToolkit's shader compiler). Output: tools\flat\<name>.fx - compile THAT file, keep the original in Effects\.
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\flatten_shader.ps1 -Shader Effects\<Name>.fx
param(
    [Parameter(Mandatory = $true)][string]$Shader,
    [string]$Root = 'C:\Users\ismai\OneDrive\Documents\My Games\Terraria\tModLoader\ModSources\IsmailsWeaponReworks'
)
$ErrorActionPreference = 'Stop'
$fxPath = Join-Path $Root $Shader
$dir = Split-Path -Parent $fxPath
$text = [IO.File]::ReadAllText($fxPath)
$text = $text.TrimStart([char]0xFEFF)
$evaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    ([IO.File]::ReadAllText((Join-Path $dir $m.Groups[1].Value))).TrimStart([char]0xFEFF)
}
$flat = [regex]::Replace($text, '(?m)^[ \t]*#include[ \t]+"([^"]+)"[^\r\n]*\r?$', $evaluator)
$outDir = Join-Path $Root 'tools\flat'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$out = Join-Path $outDir (Split-Path -Leaf $fxPath)
[IO.File]::WriteAllText($out, $flat, (New-Object System.Text.UTF8Encoding($true)))
Write-Host "Wrote $out" -ForegroundColor Green