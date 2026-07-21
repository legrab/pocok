# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0-resolute AS build
WORKDIR /src

COPY . .

RUN dotnet restore showcase/Pocok.Showcase.slnx --verbosity minimal \
    && dotnet restore showcase/Pocok.Showcase.Samples.slnx --verbosity minimal
RUN dotnet build showcase/Pocok.Showcase.slnx \
    --configuration Release \
    --no-restore \
    --verbosity minimal \
    && dotnet build showcase/Pocok.Showcase.Samples.slnx \
    --configuration Release \
    --no-restore \
    --verbosity minimal
RUN dotnet test showcase/tests/Pocok.Showcase.Tests/Pocok.Showcase.Tests.csproj \
    --configuration Release \
    --no-build \
    --verbosity minimal \
    && dotnet test samples/Showcase/Pocok.Showcase.Samples.Tests/Pocok.Showcase.Samples.Tests.csproj \
    --configuration Release \
    --no-build \
    --verbosity minimal
RUN dotnet publish src/Scripting.CSharp.Worker/Pocok.Scripting.CSharp.Worker.csproj \
    --configuration Release \
    --no-restore \
    --output /app/csharp-worker \
    -p:SelfContained=false \
    -p:UseAppHost=false \
    && cd /app/csharp-worker \
    && find . -maxdepth 1 -type f ! -name worker.sha256 -printf '%f\n' \
       | LC_ALL=C sort \
       | xargs --no-run-if-empty sha256sum > worker.sha256 \
    && test -s worker.sha256 \
    && test -f Pocok.Scripting.CSharp.Worker.dll
RUN chmod +x showcase/scripts/publish-showcase.sh \
    && bash showcase/scripts/publish-showcase.sh /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-resolute AS final
WORKDIR /app

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends python3.14 \
    && rm -rf /var/lib/apt/lists/* \
    && /usr/bin/python3.14 -I -S -c "import ast, json, sys; assert sys.version_info[:2] == (3, 14)"

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    SHOWCASE_PLUGIN_DIR=/app/plugins \
    DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_HOST_PATH=/usr/share/dotnet/dotnet \
    POCOK_CSHARP_WORKER_DIRECTORY=/app/Pocok.Scripting/CSharpWorker \
    POCOK_PYTHON_EXECUTABLE=/usr/bin/python3.14 \
    POCOK_PYTHON_WORKER_PATH=/app/Pocok.Scripting/PythonWorker/pocok_worker.py \
    DOTNET_EnableDiagnostics=0 \
    PORT=8080

COPY --from=build /app/publish .
COPY --from=build /app/csharp-worker ./Pocok.Scripting/CSharpWorker/
COPY src/Scripting.Python/Worker/pocok_worker.py ./Pocok.Scripting/PythonWorker/pocok_worker.py

RUN test -x "$DOTNET_HOST_PATH" \
    && test -x "$POCOK_PYTHON_EXECUTABLE" \
    && test -s "$POCOK_CSHARP_WORKER_DIRECTORY/worker.sha256" \
    && test -f "$POCOK_CSHARP_WORKER_DIRECTORY/Pocok.Scripting.CSharp.Worker.dll" \
    && test -f "$POCOK_PYTHON_WORKER_PATH" \
    && printf '%s' '{"ProtocolVersion":1,"Operation":"execute","Source":"21 * 2","ExpectResult":true,"Bindings":{},"AllowedImports":[],"AllowedReferencePaths":[]}' \
       | "$DOTNET_HOST_PATH" "$POCOK_CSHARP_WORKER_DIRECTORY/Pocok.Scripting.CSharp.Worker.dll" \
       | grep --quiet '"Success":true' \
    && printf '%s' '{"protocolVersion":1,"operation":"execute","source":"21 * 2","expectResult":true,"bindings":{},"allowedImports":[]}' \
       | "$POCOK_PYTHON_EXECUTABLE" -I -S "$POCOK_PYTHON_WORKER_PATH" \
       | grep --quiet '"success":true'

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pocok.Showcase.Web.dll"]
