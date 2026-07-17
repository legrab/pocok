#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "Usage: $0 --repository github.com/owner/repo [--branch main] [--app pocok] [--service showcase] [--region fra] [--instance-type free] [--strict] [--execute]"
}

repository=""
branch="main"
app="pocok"
service="showcase"
region="fra"
instance_type="free"
strict="false"
execute="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --repository) repository="${2:?Missing repository}"; shift 2 ;;
    --branch) branch="${2:?Missing branch}"; shift 2 ;;
    --app) app="${2:?Missing app name}"; shift 2 ;;
    --service) service="${2:?Missing service name}"; shift 2 ;;
    --region) region="${2:?Missing region}"; shift 2 ;;
    --instance-type) instance_type="${2:?Missing instance type}"; shift 2 ;;
    --strict) strict="true"; shift ;;
    --execute) execute="true"; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown argument: $1" >&2; usage; exit 2 ;;
  esac
done

[[ -n "$repository" ]] || { usage >&2; exit 2; }

command=(
  koyeb services create "$service"
  --app "$app"
  --git "$repository"
  --git-branch "$branch"
  --git-builder docker
  --git-docker-dockerfile showcase/Dockerfile
  --instance-type "$instance_type"
  --regions "$region"
  --env PORT=8080
  --env ASPNETCORE_ENVIRONMENT=Production
  --env "Showcase__RequireCompleteCatalog=$strict"
  --ports 8080:http
  --routes /:8080
  --checks 8080:http:/health/ready
  --wait
)

printf 'Repository: %s\nBranch: %s\nApp/service: %s/%s\nRegion: %s\nInstance: %s\nStrict catalog: %s\nCommand:' \
  "$repository" "$branch" "$app" "$service" "$region" "$instance_type" "$strict"
printf ' %q' "${command[@]}"
printf '\n'

if [[ "$execute" == "true" ]]; then
  command -v koyeb >/dev/null || { echo "Koyeb CLI is not installed." >&2; exit 1; }
  "${command[@]}"
else
  echo "Dry run only. Add --execute after authenticating the Koyeb CLI and ensuring the app exists."
fi
