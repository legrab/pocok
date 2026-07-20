# Azure Container Apps deployment

The canonical ten-plugin image can run in an Azure Container Apps consumption environment. This guide is deployment documentation, not a normal CI requirement. Azure credentials and resources are not required by repository validation.

## Prerequisites

- Azure CLI authenticated with `az login`;
- selected subscription;
- resource group;
- Container Apps environment;
- image in GitHub Container Registry or Azure Container Registry;
- registry credentials or managed identity configured for image pull.

Azure Container Apps is usage-billed. Consumption grants and free allowances can change and do not make every deployment free.

## Initial topology

- external HTTP ingress;
- target port `8080`;
- `minReplicas: 0`;
- `maxReplicas: 1`;
- no volume;
- `PORT=8080`;
- `ASPNETCORE_ENVIRONMENT=Production`;
- `Showcase__RequireCompleteCatalog=true` for the complete final image;
- startup and liveness probes on `/health/live`;
- readiness probe on `/health/ready`.

One replica avoids process-local Interactive Server circuit affinity and SignalR scale-out concerns.

## Command outline

Create the resource group and environment if they do not already exist, then adapt:

```bash
az containerapp create \
  --name pocok-showcase \
  --resource-group <resource-group> \
  --environment <container-apps-environment> \
  --image <registry>/pocok-showcase:<tag> \
  --ingress external \
  --target-port 8080 \
  --min-replicas 0 \
  --max-replicas 1 \
  --env-vars \
    PORT=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Showcase__RequireCompleteCatalog=true
```

Configure startup, liveness, and readiness probes through a reviewed Container Apps YAML or Bicep resource. Do not deploy without confirming the current CLI/YAML probe schema.

## Image update flow

1. Build the canonical Dockerfile.
2. Push a versioned image tag to GHCR or ACR.
3. Update the Container App revision to that immutable tag.
4. Wait for `/health/ready`.
5. Inspect revision logs.
6. Route traffic to the new revision or roll back to the prior healthy revision.

## Logs and operations

Use `az containerapp logs show` or the Azure portal for console logs. The application intentionally exposes no filesystem browser or raw environment dump. Scale-to-zero causes cold starts. A custom domain and managed certificate are optional.

Before increasing above one replica, revisit:

- SignalR scale-out or a managed backplane;
- session affinity;
- circuit reconnect behavior;
- deployment health during revision traffic splitting;
- memory limits and concurrency;
- catalog and plugin consistency across revisions.

Azure Static Web Apps is not a substitute. This application requires one hosted ASP.NET Core process, startup plugin registration, hosted services, temporary bounded work, and Interactive Server circuits.
