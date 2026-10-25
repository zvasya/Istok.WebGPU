#!/usr/bin/env bash
# Builds libSkiaSharp with Graphite + Dawn for one platform/arch, linked against
# the prebuilt shared Dawn prepared by build.py.
#
# usage: build-skiasharp.sh <platform> <arch> <dawn-lib>
#   platform/arch: same as build.py (Windows uses build-windows.ps1)
#   dawn-lib:      shared Dawn binary to link against (its install name / soname
#                  is what libSkiaSharp will depend on at runtime)
#
# GN arguments mirror mono/SkiaSharp@5417ed3 native/<platform>/build.cake,
# plus skia_use_dawn=true and skia_dawn_prebuilt_dir.

set -euo pipefail

PLATFORM="$1"
ARCH="$2"
DAWN_LIB="$3"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/config.sh"

DAWN_INSTALL="$DAWN_OUT_DIR/$PLATFORM-$ARCH/install"
OUT="$SKIA_OUT_DIR/$PLATFORM-$ARCH"

COMMON="
  is_official_build=true
  skia_enable_tools=false
  skia_use_dng_sdk=false
  skia_use_piex=false
  skia_use_harfbuzz=false
  skia_use_icu=false
  skia_use_partition_alloc=false
  skia_use_system_expat=false
  skia_use_system_libjpeg_turbo=false
  skia_use_system_libpng=false
  skia_use_system_libwebp=false
  skia_use_system_zlib=false
  skia_enable_skottie=true
  skia_enable_graphite=true
  skia_use_dawn=true
  skia_dawn_prebuilt_dir=\"$DAWN_INSTALL\"
  skia_dawn_prebuilt_lib=\"$DAWN_LIB\"
"

APPLE_CFLAGS="'-DSKIA_C_DLL', '-DSK_AVOID_SLOW_RASTER_PIPELINE_BLURS', '-DSK_ENABLE_LEGACY_SHADERCONTEXT', '-DHAVE_ARC4RANDOM_BUF'"

gn_cpu() {
  case "$1" in
    x86_64|x64) echo x64 ;;
    arm64|arm64-v8a) echo arm64 ;;
    armeabi-v7a) echo arm ;;
    x86) echo x86 ;;
    *) echo "unknown arch $1" >&2; exit 1 ;;
  esac
}
CPU="$(gn_cpu "$ARCH")"

case "$PLATFORM" in
  macos)
    ARGS="$COMMON
      target_os=\"mac\"
      target_cpu=\"$CPU\"
      min_macos_version=\"$MACOS_MIN\"
      skia_use_metal=true
      extra_cflags=[ $APPLE_CFLAGS, '-stdlib=libc++' ]
      extra_ldflags=[ '-stdlib=libc++', '-Wl,-rpath,@loader_path' ]
    "
    ;;
  ios|iossimulator)
    SIM=false
    [ "$PLATFORM" = iossimulator ] && SIM=true
    ARGS="$COMMON
      target_os=\"ios\"
      target_cpu=\"$CPU\"
      min_ios_version=\"$IOS_MIN\"
      ios_use_simulator=$SIM
      skia_use_metal=true
      extra_cflags=[ $APPLE_CFLAGS ]
      extra_ldflags=[ '-Wl,-rpath,@loader_path/..', '-Wl,-rpath,@executable_path/Frameworks' ]
    "
    ;;
  android)
    : "${ANDROID_NDK_HOME:?ANDROID_NDK_HOME must be set}"
    ARGS="$COMMON
      target_os=\"android\"
      target_cpu=\"$CPU\"
      ndk=\"$ANDROID_NDK_HOME\"
      ndk_api=$ANDROID_API
      skia_use_system_freetype2=false
      skia_use_vulkan=true
      extra_cflags=[ '-DSKIA_C_DLL', '-DSK_AVOID_SLOW_RASTER_PIPELINE_BLURS', '-DSK_ENABLE_LEGACY_SHADERCONTEXT', '-DHAVE_SYSCALL_GETRANDOM', '-DXML_DEV_URANDOM' ]
      extra_ldflags=[ '-Wl,--build-id=sha1', '-Wl,-z,max-page-size=16384' ]
    "
    ;;
  linux)
    # SkiaSharp builds with clang + libc++ (LINUX_STDLIB=libc++); Dawn must then be
    # built with the same CC/CXX/stdlib. Default here is the system toolchain.
    STDLIB_FLAG=""
    [ "${LINUX_STDLIB:-}" = "libc++" ] && STDLIB_FLAG="'-stdlib=libc++',"
    COMPILERS=""
    [ -n "${CC:-}" ] && COMPILERS+=" cc=\"$CC\""
    [ -n "${CXX:-}" ] && COMPILERS+=" cxx=\"$CXX\""
    ARGS="$COMMON
      target_os=\"linux\"
      target_cpu=\"$CPU\"
      $COMPILERS
      skia_use_system_freetype2=false
      skia_use_vulkan=true
      extra_asmflags=[]
      extra_cflags=[ $STDLIB_FLAG '-DSKIA_C_DLL', '-DHAVE_SYSCALL_GETRANDOM', '-DXML_DEV_URANDOM', '-DSK_AVOID_SLOW_RASTER_PIPELINE_BLURS', '-DSK_ENABLE_LEGACY_SHADERCONTEXT' ]
      extra_ldflags=[ $STDLIB_FLAG '-Wl,--version-script=$SCRIPT_DIR/linux/libSkiaSharp.map', '-Wl,-rpath,\\\$ORIGIN' ]
    "
    ;;
  *)
    echo "unknown platform: $PLATFORM" >&2
    exit 1
    ;;
esac

cd "$SKIA_DIR"
ARGS="$(echo "$ARGS" | tr '\n' ' ' | tr -s ' ' | tr "'" '"')"
bin/gn gen "$OUT" --script-executable=python3 --args="$ARGS"
ninja -j "${BUILD_JOBS:-2}" -C "$OUT" SkiaSharp

echo "Built $SKIA_DIR/$OUT"
