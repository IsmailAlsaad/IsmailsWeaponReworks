# ==============================================================================
# WEAPON REWORKS PATCH: DirectX 9 shader checker (prints the real HLSL errors of the compiler family fxcompiler.exe uses)
# Run:  powershell -ExecutionPolicy Bypass -File .\patch_shader_dx9.ps1            (apply)
#       powershell -ExecutionPolicy Bypass -File .\patch_shader_dx9.ps1 -DryRun    (verify only)
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

NewFile -File 'tools\check_shader_dx9.ps1' -Label 'new tool: compile a shader with the DirectX 9 compiler and print the real errors' -Content @'
# Compiles Effects\<Shader>.fx as ps_3_0 with the DirectX 9 shader compiler (d3dx9_NN.dll), the compiler family the XNA effect pipeline (and so
# fxcompiler.exe) uses internally, and PRINTS its real errors with the offending line. fxcompiler.exe itself prints nothing on failure.
# Writes nothing. Needs one of d3dx9_24.dll ... d3dx9_43.dll (the DirectX End-User Runtime; many PCs with games already have it).
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\check_shader_dx9.ps1 -Shader DarkFlameOrb
param(
    [Parameter(Mandatory = $true)][string]$Shader,
    [string]$Root = 'C:\Users\ismai\OneDrive\Documents\My Games\Terraria\tModLoader\ModSources\IsmailsWeaponReworks'
)
$ErrorActionPreference = 'Stop'
$Shader = [IO.Path]::GetFileNameWithoutExtension($Shader)
$effects = Join-Path $Root 'Effects'
$fx = Join-Path $effects "$Shader.fx"
if (-not (Test-Path -LiteralPath $fx)) { Write-Host "Missing $fx" -ForegroundColor Red; exit 1 }

# same flattening as compile_shader.ps1, then the technique block is cut off (only the shader function is compiled here)
$text = [IO.File]::ReadAllText($fx).TrimStart([char]0xFEFF)
$evaluator = [System.Text.RegularExpressions.MatchEvaluator] {
    param($m)
    ([IO.File]::ReadAllText((Join-Path $effects $m.Groups[1].Value))).TrimStart([char]0xFEFF)
}
$flat = [regex]::Replace($text, '(?m)^[ \t]*#include[ \t]+"([^"]+)"[^\r\n]*\r?$', $evaluator)
$flat = [regex]::Replace($flat, '(?s)technique\s+\w+.*$', '')
$lines = $flat -split "`n"

$cs = @"
using System;
using System.Runtime.InteropServices;

[ComImport, Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IDx9Buffer
{
    [PreserveSig] IntPtr GetBufferPointer();
    [PreserveSig] uint GetBufferSize();
}

public static class Dx9
{
    [DllImport("kernel32", CharSet = CharSet.Ansi)]
    static extern IntPtr LoadLibrary(string name);

    [DllImport("kernel32", CharSet = CharSet.Ansi)]
    static extern IntPtr GetProcAddress(IntPtr module, string proc);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    delegate int CompileDelegate(byte[] src, uint len, IntPtr defines, IntPtr include,
        [MarshalAs(UnmanagedType.LPStr)] string entry, [MarshalAs(UnmanagedType.LPStr)] string profile, uint flags,
        out IntPtr shader, out IntPtr errors, out IntPtr table);

    static string Text(IntPtr blob)
    {
        if (blob == IntPtr.Zero) return "";
        IDx9Buffer b = (IDx9Buffer)Marshal.GetObjectForIUnknown(blob);
        return Marshal.PtrToStringAnsi(b.GetBufferPointer(), (int)b.GetBufferSize());
    }

    // Tries d3dx9_43 down to d3dx9_24; returns the name of the dll used, or "" when none is installed.
    public static string Compile(byte[] src, string entry, string profile, out int hr, out string messages)
    {
        hr = 0;
        messages = "";
        for (int v = 43; v >= 24; v--)
        {
            string dll = "d3dx9_" + v + ".dll";
            IntPtr module = LoadLibrary(dll);
            if (module == IntPtr.Zero) continue;
            IntPtr proc = GetProcAddress(module, "D3DXCompileShader");
            if (proc == IntPtr.Zero) continue;
            CompileDelegate compile = (CompileDelegate)Marshal.GetDelegateForFunctionPointer(proc, typeof(CompileDelegate));
            IntPtr shader, errors, table;
            hr = compile(src, (uint)src.Length, IntPtr.Zero, IntPtr.Zero, entry, profile, 0, out shader, out errors, out table);
            messages = Text(errors);
            return dll;
        }
        return "";
    }
}
"@
Add-Type -TypeDefinition $cs

$bytes = [Text.Encoding]::UTF8.GetBytes($flat)
$messages = ''
$hr = 0
$dll = [Dx9]::Compile($bytes, 'PixelShaderFunction', 'ps_3_0', [ref]$hr, [ref]$messages)
if (-not $dll) {
    Write-Host 'No d3dx9_NN.dll found on this PC (looked for d3dx9_43 down to d3dx9_24). Install the "DirectX End-User Runtime" from Microsoft, then run this again.' -ForegroundColor Red
    exit 1
}
Write-Host "compiled with $dll"
if ($messages) {
    Write-Host '--- compiler messages ---'
    Write-Host $messages
    foreach ($m in [regex]::Matches($messages, '\((\d+)(?:,\d+)?(?:-\d+)?\)')) {
        $n = [int]$m.Groups[1].Value
        if ($n -ge 1 -and $n -le $lines.Count) { Write-Host ("line {0}: {1}" -f $n, $lines[$n - 1].Trim()) -ForegroundColor Yellow }
    }
}
if ($hr -lt 0) {
    Write-Host ("FAILED (HRESULT 0x{0:X8}). Paste everything above into the chat." -f $hr) -ForegroundColor Red
    exit 1
}
Write-Host 'OK: this compiler accepts it as ps_3_0.' -ForegroundColor Green
'@

try { Run-Patch } catch { Write-Host "PATCH FAILED (nothing partially applied before the write phase): $($_.Exception.Message)" -ForegroundColor Red; $global:LASTEXITCODE = 1 }
