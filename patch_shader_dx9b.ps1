# ==============================================================================
# WEAPON REWORKS PATCH: DX9 shader checker gets -NoOptimize (debug-mode compile)
# Run:  powershell -ExecutionPolicy Bypass -File .\patch_shader_dx9b.ps1            (apply)
#       powershell -ExecutionPolicy Bypass -File .\patch_shader_dx9b.ps1 -DryRun    (verify only)
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

Edit -File 'tools\check_shader_dx9.ps1' -Label 'dx9 checker edit 1' -Find @'
    [Parameter(Mandatory = $true)][string]$Shader,
'@ -Replace @'
    [Parameter(Mandatory = $true)][string]$Shader,
    [switch]$NoOptimize,
'@

Edit -File 'tools\check_shader_dx9.ps1' -Label 'dx9 checker edit 2' -Find @'
public static string Compile(byte[] src, string entry, string profile, out int hr, out string messages)
'@ -Replace @'
public static string Compile(byte[] src, string entry, string profile, uint flags, out int hr, out string messages)
'@

Edit -File 'tools\check_shader_dx9.ps1' -Label 'dx9 checker edit 3' -Find @'
hr = compile(src, (uint)src.Length, IntPtr.Zero, IntPtr.Zero, entry, profile, 0, out shader, out errors, out table);
'@ -Replace @'
hr = compile(src, (uint)src.Length, IntPtr.Zero, IntPtr.Zero, entry, profile, flags, out shader, out errors, out table);
'@

Edit -File 'tools\check_shader_dx9.ps1' -Label 'dx9 checker edit 4' -Find @'
$dll = [Dx9]::Compile($bytes, 'PixelShaderFunction', 'ps_3_0', [ref]$hr, [ref]$messages)
'@ -Replace @'
$flags = [uint32]$(if ($NoOptimize) { 5 } else { 0 })   # 5 = debug + skip optimization (what a debug build of the effect would do)
$dll = [Dx9]::Compile($bytes, 'PixelShaderFunction', 'ps_3_0', $flags, [ref]$hr, [ref]$messages)
'@

Edit -File 'tools\check_shader_dx9.ps1' -Label 'dx9 checker edit 5' -Find @'
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\check_shader_dx9.ps1 -Shader DarkFlameOrb
'@ -Replace @'
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\check_shader_dx9.ps1 -Shader DarkFlameOrb [-NoOptimize]
# -NoOptimize compiles with the optimizer off (debug mode): the code gets much longer, which is how to see whether the instruction limit is the problem.
'@

try { Run-Patch } catch { Write-Host "PATCH FAILED (nothing partially applied before the write phase): $($_.Exception.Message)" -ForegroundColor Red; $global:LASTEXITCODE = 1 }
