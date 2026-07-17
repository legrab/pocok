# Deploy to Render

`showcase/render.yaml` defines one Docker Web Service. The repository root is the build context and `showcase/Dockerfile` is the Dockerfile. No database or persistent disk is required.

## Blueprint deployment

1. Create or connect a Render account.
2. Choose **New > Blueprint**.
3. Select the repository and branch.
4. When Render asks for a Blueprint path, enter `showcase/render.yaml`.
5. Review the detected Docker Web Service.
6. Confirm:
   - plan: Free;
   - Dockerfile: `showcase/Dockerfile`;
   - build context: repository root;
   - health check: `/health/ready`;
   - `ASPNETCORE_ENVIRONMENT=Production`;
   - `Showcase__RequireCompleteCatalog=false` for Stage 1.
7. Deploy.
8. Inspect build logs for restore, tests, publication, and the discovered Conversion plugin.
9. Inspect runtime logs for startup module registration and readiness.
10. Test:
    - `/health/live`;
    - `/health/ready`;
    - `/`;
    - `/packages/conversion`;
    - `/packages/readiness`;
    - `/system`.

Render supplies `PORT`. The application validates and binds to that value on `0.0.0.0`; do not replace it with a fixed port.

## Manual dashboard deployment

1. Create a new Web Service from the repository.
2. Select Docker as the runtime.
3. Use the repository root as the build context.
4. Set the Dockerfile path to `showcase/Dockerfile`.
5. Select the Free instance.
6. Set the readiness health check to `/health/ready`.
7. Add:
   - `ASPNETCORE_ENVIRONMENT=Production`;
   - `Showcase__RequireCompleteCatalog=false`.
8. Add no disk and no database.
9. Enable automatic deploys from the selected branch after checks pass.
10. Deploy and verify the same endpoints.

## Operations

Free Web Services currently sleep after sustained inactivity and can take roughly a minute to wake. The local filesystem is ephemeral across restarts, redeploys, and spin-down. The showcase stores no durable state and has no keep-alive workaround.

A redeploy builds the current selected commit. Use Render's deployment history to inspect logs, cancel a bad build, or roll back to a known deployment. Custom domains are optional and do not change the application's port or forwarded-header requirements.

## Common failures

### Wrong Docker context

The Dockerfile copies the entire repository so it can discover package projects and shared build files. The context must be the repository root, not `showcase/`.

### Process is unreachable

Verify the application is bound to `0.0.0.0` and that Render's `PORT` environment variable is present and valid.

### Plugins are missing

Inspect the build log for the publication summary and confirm `plugins/pocok.showcase.conversion` exists in the image output. Do not add the project manually to the Dockerfile.

### Readiness returns 503

Check module diagnostics and catalog policy on `/system`. Stage 1 must use partial catalog mode. Strict mode intentionally fails until all current package slices exist.

### Free instance runs out of memory

Keep one replica, avoid additional watchers or databases, and inspect startup/runtime logs. The Stage 1 runtime is designed for one bounded worker and small immutable catalogs.

Recheck Render's current Free service, sleeping, and Blueprint documentation before relying on these limits for production.
