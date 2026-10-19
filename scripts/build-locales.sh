#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

if [[ "${1:-}" == "--no-restore" ]]; then
    restore_args=(--no-restore)
elif [[ $# -eq 0 ]]; then
    dotnet restore SpatialAim.Filters.sln
    restore_args=(--no-restore)
else
    printf 'Usage: %s [--no-restore]\n' "$0" >&2
    exit 2
fi

locales=(en es ja zh-CN ko de pl fr)

for locale in "${locales[@]}"; do
    printf 'Testing and building locale: %s\n' "$locale"
    dotnet test SpatialAim.Filters.sln \
        --configuration Release \
        "${restore_args[@]}" \
        -p:SpatialAimLocale="$locale"
    dotnet build src/SpatialAim.Filters/SpatialAim.Filters.csproj \
        --configuration Release \
        "${restore_args[@]}" \
        -p:SpatialAimLocale="$locale"

    locale_dir="artifacts/locales/$locale"
    mkdir -p "$locale_dir"
    cp src/SpatialAim.Filters/bin/Release/net8.0/SpatialAim.Filters.dll "$locale_dir/SpatialAim.Filters.dll"

    archive="artifacts/SpatialAim.Filters-$locale.zip"
    if [[ "$locale" == "en" ]]; then
        archive="artifacts/SpatialAim.Filters.zip"
    fi
    python3 - "$locale_dir/SpatialAim.Filters.dll" "$archive" <<'PY'
import sys
import zipfile
from pathlib import Path

dll_path = Path(sys.argv[1])
archive_path = Path(sys.argv[2])
archive_path.parent.mkdir(parents=True, exist_ok=True)
with zipfile.ZipFile(archive_path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
    archive.write(dll_path, arcname=dll_path.name)
PY
done

dotnet build src/SpatialAim.Filters/SpatialAim.Filters.csproj \
    --configuration Release \
    "${restore_args[@]}" \
    -p:SpatialAimLocale=en