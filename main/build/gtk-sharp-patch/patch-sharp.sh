#!/bin/bash
set -euo pipefail

DIR=${1:-/usr/lib/mono/gtk-sharp-2.0}
OUT_DIR=${2:-$(dirname "$0")}
HERE=$(dirname "$0")
CS='/home/daniel/.dotnet/dotnet /home/daniel/.dotnet/sdk/10.0.401/Roslyn/bincore/csc.dll -nologo -target:exe'
REFS='-r:/home/daniel/.dotnet/shared/Microsoft.NETCore.App/10.0.12/System.Runtime.dll -r:/home/daniel/.dotnet/shared/Microsoft.NETCore.App/10.0.12/System.Console.dll -r:/home/daniel/.dotnet/shared/Microsoft.NETCore.App/10.0.12/System.Private.CoreLib.dll -r:/home/daniel/.dotnet/shared/Microsoft.NETCore.App/10.0.12/System.Collections.dll'
$CS -out:"$HERE/sortfix2built.dll" $REFS "$HERE/sortfix2.cs" >/dev/null
cp "$HERE/sortfix2.runtimeconfig.json" "$HERE/sortfix2built.runtimeconfig.json"

WORK=$(mktemp -d)
trap 'rm -rf "$WORK"' EXIT

for dll in gtk-sharp glade-sharp gdk-sharp; do
    monodis --output="$WORK/$dll.il" "$DIR/$dll.dll"
    if python3 "$HERE/patch-sharp.py" "$WORK/$dll.il" "$WORK/$dll-patched.il"; then
        ilasm -dll -output="$WORK/$dll-raw.dll" "$WORK/$dll-patched.il" >/dev/null
        /home/daniel/.dotnet/dotnet exec "$HERE/sortfix2built.dll" "$WORK/$dll-raw.dll" >/dev/null
        cp "$WORK/$dll-raw.dll.fix" "$OUT_DIR/$dll-dll.dll"
        echo "patched+fixed $dll -> $OUT_DIR/$dll-dll.dll"
    else
        echo "skipped $dll (no pattern)"
    fi
done
