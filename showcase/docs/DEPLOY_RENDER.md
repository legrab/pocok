# Deploy to Render

The public Showcase runs at [pocok-showcase.onrender.com](https://pocok-showcase.onrender.com/).

`showcase/render.yaml` defines one Docker Web Service. The repository root is the build context and `showcase/Dockerfile` is the Dockerfile. The application requires no database or persistent disk.

## Blueprint deployment

1. Create or connect a Render account.
2. Choose **New > Blueprint**.
3. Select the repository and branch.
4. Use `showcase/render.yaml` as the Blueprint path.
5. Review the detected Docker Web Service.
6. Confirm:
   - plan: Free;
   - Dockerfile: `showcase/Dockerfile`;
   - build context: repository root;
   - health check: `/health/ready`;
   - `ASPNETCORE_ENVIRONMENT=Production`;
   - `Showcase__RequireCompleteCatalog=false` while the deployment contains only selected package examples.
7. Deploy.
8. Inspect the build log for restore, tests, publication, and plugin discovery.
9. Inspect the runtime log for module registration and readiness.
10. Verify:
    - `/health/live`;
    - `/health/ready`;
    - `/`;
    - `/packages/conversion`;
    - `/packages/scripting`;
    - `/packages/licensing`;
    - `/packages/readiness`;
    - `/system`.

Render supplies `PORT`. The application validates that value and binds to `0.0.0.0`; do not replace it with a fixed port.

## Manual dashboard deployment

1. Create a Web Service from the repository.
2. Select Docker as the runtime.
3. Use the repository root as the build context.
4. Set the Dockerfile path to `showcase/Dockerfile`.
5. Select the Free instance.
6. Set the health check to `/health/ready`.
7. Add:
   - `ASPNETCORE_ENVIRONMENT=Production`;
   - `Showcase__RequireCompleteCatalog=false`.
8. Add no disk and no database.
9. Enable automatic deploys from the selected branch after checks pass.
10. Deploy and verify the same endpoints.

## Keepalive workflow

`.github/workflows/showcase-keepalive.yml` requests the deployed root page at minutes `7,19,31,43,55` of every hour. It exists only to reduce free-tier spin-down during active development and demonstration periods.

The workflow is not part of application correctness. Health checks, startup validation, and deployment smoke tests must still pass without it.

## Operations

The application stores no durable state, so restarts and redeploys do not require a persistent volume. A redeploy builds the selected commit. Use Render deployment history to inspect logs, cancel a bad build, or roll back.

Custom domains are optional and do not change port handling or forwarded-header requirements.

## Common failures

### Wrong Docker context

The Dockerfile copies the repository because publication needs package projects and shared build files. The context must be the repository root, not `showcase/`.

### Process is unreachable

Verify that Render provides a valid `PORT` value and that the application binds to `0.0.0.0`.

### Plugins are missing

Inspect the publication summary and confirm that the final image contains isolated plugin directories for Conversion, Scripting, and Licensing. Plugin projects must not be referenced directly by the Web host.

### Readiness returns 503

Check module diagnostics and catalog policy on `/system`. Partial catalog mode is required until every catalog package has an installed Showcase plugin.

### Free instance runs out of memory

Keep one replica, avoid additional watchers or databases, and inspect startup and runtime logs. Sandbox execution is bounded, but the service still shares the memory limit of the Render instance.

Review Render's current plan and sleeping behavior before treating the free deployment as production infrastructure.
