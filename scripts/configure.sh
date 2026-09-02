#!/usr/bin/env bash

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

if command -v mono >/dev/null 2>&1 && command -v mcs >/dev/null 2>&1; then
	if [ ! -f "$DIR/configure.exe" ]; then
		mcs "$DIR/configure.cs" -out:"$DIR/configure.exe"
	fi
	LANG=C mono "$DIR/configure.exe" "$@"
	exit $?
fi

DOTNET_BIN="$(command -v dotnet 2>/dev/null || true)"
if [ -z "$DOTNET_BIN" ] && [ -x "$HOME/.dotnet/dotnet" ]; then
	DOTNET_BIN="$HOME/.dotnet/dotnet"
fi
if [ -z "$DOTNET_BIN" ] && [ -x "/usr/share/dotnet/dotnet" ]; then
	DOTNET_BIN="/usr/share/dotnet/dotnet"
fi

if [ -n "$DOTNET_BIN" ]; then
	if [ ! -f "$DIR/configure.dll" ] || [ ! -f "$DIR/configure.runtimeconfig.json" ]; then
		"$DOTNET_BIN" build "$DIR/configure.net8.csproj" -nologo -v minimal -p:OutputPath="$DIR/" -p:AppendTargetFrameworkToOutputPath=false
		if [ $? -ne 0 ]; then
			exit $?
		fi
	fi
	"$DOTNET_BIN" "$DIR/configure.dll" "$@"
	exit $?
fi

echo "Mono or the .NET SDK is required to run the MonoDevelop bootstrap configuration tool." >&2
exit 1
