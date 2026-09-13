#!/bin/bash
set -euo pipefail

GLIB_SRC=${1:-/usr/lib/mono/gtk-sharp-2.0/glib-sharp.dll}
OUT_DIR=${2:-$(dirname "$0")}
WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

monodis --output="$WORK/glib-sharp.il" "$GLIB_SRC"
python3 "$(dirname "$0")/patch.py" "$WORK/glib-sharp.il" "$WORK/glib-sharp-patched.il"
ilasm -dll -output="$OUT_DIR/glib-sharp-fv2.dll" "$WORK/glib-sharp-patched.il" >/dev/null

echo "patched glib-sharp written to $OUT_DIR/glib-sharp-fv2.dll"