# ==============================================================================
# WEAPON REWORKS PATCH: shader bisect tool (which feature does the XNA effect compiler reject?)
# Run:  powershell -ExecutionPolicy Bypass -File .\patch_shader_bisect.ps1            (apply)
#       powershell -ExecutionPolicy Bypass -File .\patch_shader_bisect.ps1 -DryRun    (verify only)
# Engine lives in the repo at tools\patch_engine.ps1 (all-or-nothing, see its header).
# ==============================================================================
param(
    [string]$Root = 'C:\Users\ismai\OneDrive\Documents\My Games\Terraria\tModLoader\ModSources\IsmailsWeaponReworks',
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'
$engine = Join-Path $Root 'tools\patch_engine.ps1'
if (-not (Test-Path -LiteralPath $engine)) { Write-Host "Missing $engine - copy/commit tools\patch_engine.ps1 first." -ForegroundColor Red; exit 1 }
. $engine

NewFile -File 'tools\bisect_shader.ps1' -Label 'new tool: compile shader variants to find what the compiler rejects' -Content @'
# Finds out WHICH feature of Effects\<Shader>.fx the XNA effect compiler rejects: builds variants of the shader with one suspect changed each,
# compiles every one with tools\compile_shader.ps1 and prints a table. Writes only temporary files (Effects\<Shader>T1.fx ...), removed at the end.
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\bisect_shader.ps1 -Shader DarkFlameOrb
param(
    [Parameter(Mandatory = $true)][string]$Shader,
    [string]$Root = 'C:\Users\ismai\OneDrive\Documents\My Games\Terraria\tModLoader\ModSources\IsmailsWeaponReworks'
)
$ErrorActionPreference = 'Stop'
$Shader = [IO.Path]::GetFileNameWithoutExtension($Shader)
$effects = Join-Path $Root 'Effects'
$compile = Join-Path $Root 'tools\compile_shader.ps1'
$src = [IO.File]::ReadAllText((Join-Path $effects "$Shader.fx")).TrimStart([char]0xFEFF)

$loopRx = '\[loop\]\s*for \(int i = 0; i < 10; i\+\+\)'
$sincosRx = 'sincos\(-uOrbit\.z \* dt, rs, rc\);'
$sincosNew = 'rs = sin(-uOrbit.z * dt); rc = cos(-uOrbit.z * dt);'
function NoUniforms($s) {
    $s = $s -replace 'float4 uOrbit;', 'static float4 uOrbit = float4(0.5, 0.3, 2.0, 1.0);'
    return ($s -replace 'float2 uOwnerVel;', 'static float2 uOwnerVel = float2(0.1, 0.1);')
}
$unroll2 = $src -replace $loopRx, 'for (int i = 0; i < 2; i++)'
$variants = [ordered]@{
    'T1  loop unrolled to 2 embers (no [loop])'                    = $unroll2
    'T2  no embers at all (base: ball, halo, uniforms)'            = ($src -replace $loopRx, 'for (int i = 0; i < 0; i++)')
    'T3  2 unrolled embers, sin/cos instead of sincos'             = ($unroll2 -replace $sincosRx, $sincosNew)
    'T4  [loop] x10, sin/cos instead of sincos'                    = ($src -replace $sincosRx, $sincosNew)
    'T5  [loop] x10, orbit values as constants (no custom uniforms)' = (NoUniforms $src)
    'T6  2 unrolled embers, no custom uniforms'                    = (NoUniforms $unroll2)
}

$names = @()
try {
    $n = 0
    $results = @()
    foreach ($key in $variants.Keys) {
        $n++
        $name = "${Shader}T$n"
        $names += $name
        [IO.File]::WriteAllText((Join-Path $effects "$name.fx"), $variants[$key], (New-Object System.Text.UTF8Encoding($true)))
        & powershell -NoProfile -ExecutionPolicy Bypass -File $compile -Shader $name -Root $Root *> $null
        $ok = ($LASTEXITCODE -eq 0)
        $results += [pscustomobject]@{ Result = $(if ($ok) { 'COMPILES' } else { 'fails' }); Variant = $key }
        Write-Host ("{0,-9} {1}" -f $results[-1].Result, $key) -ForegroundColor $(if ($ok) { 'Green' } else { 'Red' })
    }
}
finally {
    foreach ($name in $names) {
        Remove-Item -LiteralPath (Join-Path $effects "$name.fx"), (Join-Path $effects "$name.xnb") -ErrorAction SilentlyContinue
    }
}
Write-Host 'Paste these lines into the chat.'
'@

try { Run-Patch } catch { Write-Host "PATCH FAILED (nothing partially applied before the write phase): $($_.Exception.Message)" -ForegroundColor Red; $global:LASTEXITCODE = 1 }
