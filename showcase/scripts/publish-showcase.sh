#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"
output_path="${1:-$repo_root/artifacts/showcase}"
if [[ $# -gt 0 ]]; then
  shift
fi

unset PLATFORM || true
dotnet_path="${DOTNET_HOST_PATH:-dotnet}"
tool_project="$repo_root/showcase/tools/Pocok.Showcase.PublishTool/Pocok.Showcase.PublishTool.csproj"
tool_dll="$repo_root/showcase/tools/Pocok.Showcase.PublishTool/bin/Release/net10.0/Pocok.Showcase.PublishTool.dll"
no_restore=false
require_complete=false

for argument in "$@"; do
  case "$argument" in
    --no-restore) no_restore=true ;;
    --require-complete) require_complete=true ;;
    *) echo "Unknown argument: $argument" >&2; exit 2 ;;
  esac
done

build_args=(build "$tool_project" --configuration Release --nologo --maxcpucount:1)
if [[ "$no_restore" == true ]]; then
  build_args+=(--no-restore)
fi
"$dotnet_path" "${build_args[@]}"

publish_args=(
  "$tool_dll"
  --repository-root "$repo_root"
  --output "$output_path"
  --dotnet "$dotnet_path"
)
if [[ "$no_restore" == true ]]; then
  publish_args+=(--no-restore)
fi
if [[ "$require_complete" == true ]]; then
  publish_args+=(--require-complete)
fi

"$dotnet_path" "${publish_args[@]}"
