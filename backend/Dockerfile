# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props InspectFlow.slnx ./
COPY src/Shared/InspectFlow.Shared.csproj src/Shared/
COPY src/Modules/InspectFlow.Modules.csproj src/Modules/
COPY src/Infrastructure/InspectFlow.Infrastructure.csproj src/Infrastructure/
COPY src/Api/InspectFlow.Api.csproj src/Api/
# Optional: trust an extra CA (corporate TLS proxy): docker build --secret id=extra_ca,src=/path/ca.crt
RUN --mount=type=secret,id=extra_ca,required=false \
    if [ -f /run/secrets/extra_ca ]; then cp /run/secrets/extra_ca /usr/local/share/ca-certificates/extra.crt && update-ca-certificates; fi; \
    dotnet restore src/Api/InspectFlow.Api.csproj
COPY src/ src/
RUN dotnet publish src/Api/InspectFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# QuestPDF renders with its bundled Skia; fontconfig lets it resolve fonts.
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /data/storage && chown -R app:app /data
USER app
ENV ASPNETCORE_URLS=http://+:8080 \
    STORAGE_LOCAL_PATH=/data/storage \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --retries=10 CMD curl -fs http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "InspectFlow.Api.dll"]
