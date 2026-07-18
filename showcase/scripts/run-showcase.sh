#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"
temporary="$(mktemp -d "${TMPDIR:-/tmp}/pocok-showcase.XXXXXX")"
cleanup() { rm -rf -- "$temporary"; }
trap cleanup EXIT INT TERM

unset PLATFORM || true
"$script_dir/publish-showcase.sh" "$temporary" "$@"
export PORT="${PORT:-8080}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export SHOWCASE_PLUGIN_DIR="$temporary/plugins"

printf 'Pocok Showcase: http://127.0.0.1:%s\n' "$PORT"
"${DOTNET_HOST_PATH:-dotnet}" "$temporary/Pocok.Showcase.Web.dll"
