# Checks Effects\<Shader>.fx with Windows' own HLSL compiler (d3dcompiler_47.dll, always present on Windows 10/11) as ps_3_0.
# Unlike tools\compile_shader.ps1 it PRINTS the real errors (with the offending line) and the instruction-slot count. It writes nothing.
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\check_shader.ps1 -Shader DarkFlameOrb
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
public interface ID3DBlob
{
    [PreserveSig] IntPtr GetBufferPointer();
    [PreserveSig] IntPtr GetBufferSize();
}

public static class D3DC
{
    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int D3DCompile(byte[] src, IntPtr size, string name, IntPtr defines, IntPtr include, string entry, string target, uint f1, uint f2, out IntPtr code, out IntPtr errors);

    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall)]
    static extern int D3DDisassemble(IntPtr src, IntPtr size, uint flags, string comments, out IntPtr text);

    static string BlobText(IntPtr blob)
    {
        if (blob == IntPtr.Zero) return "";
        ID3DBlob b = (ID3DBlob)Marshal.GetObjectForIUnknown(blob);
        return Marshal.PtrToStringAnsi(b.GetBufferPointer(), b.GetBufferSize().ToInt32());
    }

    // Returns the compiler messages (errors and warnings) and, when it compiled, the disassembly in 'listing'.
    public static int Compile(byte[] src, string name, string entry, string target, out string messages, out string listing)
    {
        IntPtr code, errors;
        int hr = D3DCompile(src, new IntPtr(src.Length), name, IntPtr.Zero, IntPtr.Zero, entry, target, 0, 0, out code, out errors);
        messages = BlobText(errors);
        listing = "";
        if (hr >= 0 && code != IntPtr.Zero)
        {
            ID3DBlob b = (ID3DBlob)Marshal.GetObjectForIUnknown(code);
            IntPtr text;
            if (D3DDisassemble(b.GetBufferPointer(), b.GetBufferSize(), 0, null, out text) >= 0)
                listing = BlobText(text);
        }
        return hr;
    }
}
"@
Add-Type -TypeDefinition $cs

$bytes = [Text.Encoding]::UTF8.GetBytes($flat)
$messages = ''
$listing = ''
$hr = [D3DC]::Compile($bytes, "$Shader.fx", 'PixelShaderFunction', 'ps_3_0', [ref]$messages, [ref]$listing)

if ($messages) {
    Write-Host '--- compiler messages ---'
    Write-Host $messages
    foreach ($m in [regex]::Matches($messages, '\((\d+),(\d+)(?:-\d+)?\)')) {
        $n = [int]$m.Groups[1].Value
        if ($n -ge 1 -and $n -le $lines.Count) { Write-Host ("line {0}: {1}" -f $n, $lines[$n - 1].Trim()) -ForegroundColor Yellow }
    }
}
if ($hr -lt 0) {
    Write-Host ("FAILED (HRESULT 0x{0:X8}). Paste everything above into the chat." -f $hr) -ForegroundColor Red
    exit 1
}
$slots = [regex]::Match($listing, 'approximately (\d+) instruction slots used')
if ($slots.Success) { Write-Host ("OK: compiles as ps_3_0, about {0} instruction slots (the ps_3_0 limit is 512)." -f $slots.Groups[1].Value) -ForegroundColor Green }
else { Write-Host 'OK: compiles as ps_3_0.' -ForegroundColor Green }