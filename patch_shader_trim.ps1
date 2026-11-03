# ==============================================================================
# WEAPON REWORKS PATCH: DarkFlameOrb shader trim - under the 512 instruction slots of ps_3_0 (was about 529)
# Run:  powershell -ExecutionPolicy Bypass -File .\patch_shader_trim.ps1            (apply)
#       powershell -ExecutionPolicy Bypass -File .\patch_shader_trim.ps1 -DryRun    (verify only)
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

Edit -File 'Effects\DarkFlameOrb.fx' -Label 'shader trim: 3-octave warp noise' -Find @'
    float n1 = fbm4(sp);
'@ -Replace @'
    float n1 = fbm3(sp);                                    // 3 octaves: the shader must stay under the 512 slots of ps_3_0
'@

Edit -File 'Effects\DarkFlameOrb.fx' -Label 'shader trim: constant halo edge' -Find @'
    float gEdge = exp(-reach * gk);
'@ -Replace @'
    float gEdge = 0.049787;                                 // = exp(-3): reach * gk is always 3, so no need to compute it
'@

Edit -File 'Effects\DarkFlameOrb.fx' -Label 'shader trim: sincos for the break-off direction' -Find @'
        float ang = -1.5708 + (r.x - 0.5) * 2.4;            // breaks off somewhere on the upper rim
        float2 start = float2(cos(ang), sin(ang)) * (R * 0.8 * grow);
'@ -Replace @'
        float so, co;
        sincos((r.x - 0.5) * 2.4, so, co);                  // breaks off somewhere on the upper rim, within +-1.2 rad of straight up
        float2 start = float2(so, -co) * (R * 0.8 * grow);
'@

try { Run-Patch } catch { Write-Host "PATCH FAILED (nothing partially applied before the write phase): $($_.Exception.Message)" -ForegroundColor Red; $global:LASTEXITCODE = 1 }
