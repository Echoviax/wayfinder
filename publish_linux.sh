#!/bin/bash
DIST="./Dist/Linux"
SUBDIR="$DIST/Wayfinder"
TEMP_BINS="$DIST/temp_bins"

echo "[1/4] Cleaning..."
rm -rf "$DIST"
mkdir -p "$SUBDIR"

echo "[2/4] Publishing Launcher..."
dotnet publish Wayfinder.Launcher -c Release -r linux-x64 --self-contained false -p:PublishSingleFile=true -o "$DIST"

echo "[3/4] Publishing Mod Loader..."
dotnet publish Wayfinder.Patcher -c Release --self-contained false -o "$SUBDIR"
dotnet publish Wayfinder.Core -c Release --self-contained false -o "$SUBDIR"

echo "[4/4] Grabbing Runtime Files..."
dotnet publish Wayfinder.Patcher -c Release -r linux-x64 --self-contained true -o "$TEMP_BINS"

cp "$TEMP_BINS/libclrjit.so" "$SUBDIR/"
cp "$TEMP_BINS/libcoreclr.so" "$SUBDIR/"
cp "$TEMP_BINS/libhostpolicy.so" "$SUBDIR/"

rm -rf "$TEMP_BINS"
rm -f "$SUBDIR/Wayfinder.Patcher"
rm -f "$SUBDIR"/*.json

echo "Setting permissions..."
chmod +x "$DIST/Wayfinder.Launcher"

echo "Done! Build located in $DIST"