# Deploy to Koyeb

Koyeb can build `showcase/Dockerfile` from GitHub and expose the Interactive Server WebSocket connection through one Web Service.

## Dashboard

1. Create a Koyeb account.
2. Connect GitHub, or use a public GitHub repository.
3. Create an application if one does not already exist.
4. Create a Web Service and choose GitHub as the source.
5. Choose the Dockerfile builder.
6. Keep the repository root as the build work directory.
7. Set the Dockerfile path to `showcase/Dockerfile`.
8. Select the branch.
9. Expose `8080` as HTTP and route `/` to port `8080`.
10. Set:
    - `PORT=8080`;
    - `ASPNETCORE_ENVIRONMENT=Production`;
    - `Showcase__RequireCompleteCatalog=false`.
11. Add an HTTP health check on port `8080` at `/health/ready`.
12. Select the Free instance and Frankfurt (`fra`) by default for a likely European audience.
13. Deploy and inspect build/runtime logs.
14. Test:
    - `/health/live`;
    - `/health/ready`;
    - `/`;
    - `/packages/conversion`;
    - `/packages/readiness`;
    - `/system`.

The Free instance currently provides limited CPU, 512 MB RAM, and ephemeral storage. One Free instance is allowed per organization and it can scale to zero after inactivity. Expect cold starts. Keep one replica because Blazor Interactive Server circuits are process-local and use WebSockets.

## CLI scripts

The scripts accept repository, branch, app, service, region, instance type, and catalog policy. They print the resolved command and run only with explicit execution.

Authenticate the Koyeb CLI and ensure the named application exists.

```bash
bash showcase/scripts/deploy-koyeb.sh \
  --repository github.com/OWNER/REPOSITORY \
  --branch main \
  --app pocok \
  --service showcase \
  --region fra \
  --instance-type free

# Repeat with --execute after reviewing the command.
# Add --strict only after all current package slices exist.
```

```powershell
./showcase/scripts/deploy-koyeb.ps1 `
  -Repository github.com/OWNER/REPOSITORY `
  -Branch main `
  -App pocok `
  -Service showcase `
  -Region fra `
  -InstanceType free

# Repeat with -Execute after reviewing the command.
# Add -Strict only after all current package slices exist.
```

The generated command uses the Docker builder, `showcase/Dockerfile`, `8080:http`, route `/:8080`, the required environment variables, and `/health/ready`.

## Troubleshooting

- **Build cannot find repository files:** keep the repository root as the build directory.
- **Service starts but cannot be reached:** verify `PORT=8080`, HTTP port `8080`, and route `/:8080`.
- **Blazor disconnects:** verify WebSocket traffic is allowed and keep one replica.
- **Readiness is 503:** keep partial catalog mode for Stage 1 and inspect `/system`.
- **Plugin missing:** inspect publication output in build logs; do not hardcode a sample project list.
- **CLI command fails:** run `koyeb services create --help` for the installed CLI version and use the dashboard flow as the dependable fallback.

Recheck current Koyeb region, instance, CLI, and scale-to-zero documentation before production use.
