#!/usr/bin/env bash
set -euo pipefail

project_name=${1:?Pass the GameCI build name}
player_dir=build/StandaloneWindows64
player_exe="${project_name}.exe"
data_dir="${project_name}_Data"
archive="$PWD/${project_name}-Windows.zip"

if [[ ! -f "$player_dir/$player_exe" ]]; then
    echo "Missing Windows player: $player_dir/$player_exe" >&2
    exit 1
fi

if [[ ! -d "$player_dir/$data_dir" ]]; then
    echo "Missing Unity data directory: $player_dir/$data_dir" >&2
    exit 1
fi

rm -f "$archive"
cd "$player_dir"
zip -qr "$archive" .
test -s "$archive"
unzip -tq "$archive"
# Require the game executable and its data at the root of the ZIP.
unzip -Z -1 "$archive" "$player_exe" "$data_dir/"
