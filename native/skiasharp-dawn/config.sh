# Settings for the Unix SkiaSharp build helper called by build.py.
: "${SKIA_DIR:?SKIA_DIR must point to the prepared Skia checkout}"
: "${DAWN_OUT_DIR:?DAWN_OUT_DIR must contain the prebuilt Dawn installs}"
SKIA_OUT_DIR="${SKIA_OUT_DIR:-out/istok}"
MACOS_MIN="11.0"
IOS_MIN="14.0"
ANDROID_API="24"
