# Modular communicator sample

This sample keeps the host dependent only on an application-owned communicator contract and `Pocok.Modularity`. The echo implementation is built and deployed separately as a plugin directory.

```pwsh
./samples/ModularCommunicator/Stage-Plugin.ps1

# Then run the exact command printed by the staging script. It passes the staged
# plugin root explicitly instead of relying on the process working directory.
```

The staging script is sample tooling, not runtime plugin installation. Production deployment should copy a reviewed plugin directory through the application's normal deployment process.
