<#
.SYNOPSIS
  Builds one or more Windows SkiaSharp slices against installed shared Dawn.
  Called by build.py after source checkout and Dawn compilation.
.REQUIREMENTS
  Visual Studio 2022 C++ tools (including requested architectures), LLVM,
  Ninja, Python 3 and the Windows SDK selected by the CI workflow.
#>
param(
  [Parameter(Mandatory)] [string[]] $Arch,
  [Parameter(Mandatory)] [string] $SkiaDir,
  [Parameter(Mandatory)] [string] $DawnOutDir,
  [string] $LlvmHome = $(if ($env:LLVM_HOME) { $env:LLVM_HOME } else { "C:/Program Files/LLVM" }),
  [int] $Jobs = 2
)
$ErrorActionPreference = "Stop"

function Invoke-Checked([string] $exe, [string[]] $argv) {
  & $exe @argv
  if ($LASTEXITCODE -ne 0) { throw "$exe failed with exit code $LASTEXITCODE" }
}

function Invoke-InVcVars([string] $vcArch, [string] $command) {
  $vswhere = "${env:ProgramFiles(x86)}/Microsoft Visual Studio/Installer/vswhere.exe"
  $vs = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
  if (-not $vs) { throw "Visual Studio with C++ tools not found" }
  $vcvars = "$vs/VC/Auxiliary/Build/vcvarsall.bat"
  cmd /c "`"$vcvars`" $vcArch >nul && $command"
  if ($LASTEXITCODE -ne 0) { throw "Command failed ($vcArch): $command" }
}

$SkiaDir = (Resolve-Path $SkiaDir).Path -replace '\\', '/'
$DawnOutDir = (Resolve-Path $DawnOutDir).Path -replace '\\', '/'

foreach ($a in $Arch) {
  $vcArch = @{ "x64" = "amd64"; "x86" = "amd64_x86"; "arm64" = "amd64_arm64" }[$a]
  if (-not $vcArch) { throw "Unknown arch $a" }
  $dawnInstall = "$DawnOutDir/windows-$a/install"
  $dawnLib = "$dawnInstall/lib/webgpu_dawn.lib"
  if (-not (Test-Path $dawnLib)) { throw "Missing $dawnLib" }

  # ----------------------------------------------------------- libSkiaSharp
  $out = "out/istok/windows-$a"
  $gnArgs = @(
    "target_os=`"win`"", "target_cpu=`"$a`"",
    "is_official_build=true", "skia_enable_tools=false",
    "clang_win=`"$($LlvmHome -replace '\\', '/')`"",
    "skia_enable_fontmgr_win_gdi=false", "skia_enable_fontmgr_win=true",
    "skia_use_harfbuzz=false", "skia_use_icu=false", "skia_use_partition_alloc=false",
    "skia_use_dng_sdk=false", "skia_use_piex=false",
    "skia_use_system_expat=false", "skia_use_system_freetype2=false",
    "skia_use_system_libjpeg_turbo=false", "skia_use_system_libpng=false",
    "skia_use_system_libwebp=false", "skia_use_system_zlib=false",
    "skia_use_freetype=false",
    "skia_enable_skottie=true",
    "skia_enable_graphite=true",
    "skia_use_vulkan=true", "skia_use_direct3d=true",
    "skia_use_dawn=true",
    "skia_dawn_prebuilt_dir=`"$dawnInstall`"",
    "skia_dawn_prebuilt_lib=`"$dawnLib`"",
    "extra_cflags=[ `"-DSKIA_C_DLL`", `"-DSK_AVOID_SLOW_RASTER_PIPELINE_BLURS`", `"-DSK_ENABLE_LEGACY_SHADERCONTEXT`", `"/MT`", `"/EHsc`", `"/Z7`", `"/guard:cf`", `"-D_HAS_AUTO_PTR_ETC=1`" ]",
    "extra_ldflags=[ `"/DEBUG:FULL`", `"/DEBUGTYPE:CV,FIXUP`", `"/guard:cf`", `"/DELAYLOAD:d3d12.dll`", `"/DELAYLOAD:dxgi.dll`", `"/DELAYLOAD:D3DCOMPILER_47.dll`", `"/DEFAULTLIB:delayimp`" ]"
  ) -join " "

  Push-Location $SkiaDir
  $gnArgsFile = New-TemporaryFile
  Set-Content -Path $gnArgsFile -Value $gnArgs -NoNewline
  try {
    Invoke-Checked "bin/gn.exe" @("gen", $out, "--script-executable=python", "--args=$(Get-Content -Raw $gnArgsFile)")
  } finally { Remove-Item $gnArgsFile }
  Invoke-InVcVars $vcArch "ninja -j $Jobs -C $out SkiaSharp"
  Pop-Location

}
