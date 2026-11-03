# ==============================================================================
# WEAPON REWORKS PATCH: roadmap - honest status of Dark Lance fix round 1 (shader does not compile yet)
# Run:  powershell -ExecutionPolicy Bypass -File .\patch_roadmap_status.ps1            (apply)
#       powershell -ExecutionPolicy Bypass -File .\patch_roadmap_status.ps1 -DryRun    (verify only)
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

Edit -File 'docs\ROADMAP.md' -Label 'Roadmap: progress line status' -Find @'
(applied, untested; shaders `DarkFlameOrb` and the new `PixelGlow` must be compiled)
'@ -Replace @'
(C# applied, untested; `PixelGlow` compiles, `DarkFlameOrb` does NOT compile with fxcompiler yet, see the status in its section)
'@

Edit -File 'docs\ROADMAP.md' -Label 'Roadmap: Dark Lance fix round 1 status' -Find @'
### Dark Lance fix round 1 (2026-11-04, applied, untested)
'@ -Replace @'
### Dark Lance fix round 1 (2026-11-04, applied, untested)

STATUS (end of chat): all C# is applied (smooth tip glow via `PixelGlow`, orb trail on `PixelTrail`, `DrawOrb` passes the orbit data). `PixelGlow.xnb` compiled. `DarkFlameOrb.fx` currently holds a simplified STAGE 1 (6 embers, no "stay behind in the world", no uParams2 / uOrbit / uOwnerVel use) and `fxcompiler.exe` still rejects it silently (it prints nothing after "Compiling font...", exit code 1, no .xnb). The OLD `DarkFlameOrb.xnb` is still in `Effects`, so the game shows the old orb until a new one compiles. Facts found: the DirectX 9 compiler (`tools\check_shader_dx9.ps1`, d3dx9_43) accepts the stage 1 shader, also with `-NoOptimize`; the Windows compiler (`tools\check_shader.ps1`) accepts it too; `tools\bisect_shader.ps1` showed the base (ball, halo, no embers) compiles and EVERY variant with an ember loop fails (unrolled or `[loop]`, sin/cos or sincos, with or without the custom uniforms). So it is not size, not `[loop]`, not sincos, not the custom uniforms. Next ideas: (1) mimic the whole-effect compile with `D3DXCreateEffectCompiler` (includes the technique block, which the checkers cut off); (2) bisect INSIDE one ember (remove sqrt, frac(float2), floor, uSeed use, the division by max(dot,...)); (3) as a last resort draw the embers from C# as `PixelLine`/pixel squares and keep the shader for the ball and halo only. New chat prompt: "continue the Dark Lance fix round 1 in the docs/roadmap: DarkFlameOrb.fx will not compile with fxcompiler (see STATUS), find out why".
'@

try { Run-Patch } catch { Write-Host "PATCH FAILED (nothing partially applied before the write phase): $($_.Exception.Message)" -ForegroundColor Red; $global:LASTEXITCODE = 1 }
