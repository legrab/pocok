# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
RUN chmod +x showcase/scripts/publish-showcase.sh \
    && bash showcase/scripts/publish-showcase.sh /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:8080 \
    SHOWCASE_PLUGIN_DIR=/app/plugins \
    DOTNET_EnableDiagnostics=0 \
    PORT=8080

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Pocok.Showcase.Web.dll"]
