FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG SERVICE_PROJECT
ARG SERVICE_DLL
WORKDIR /src

COPY . .

RUN dotnet restore "${SERVICE_PROJECT}"
RUN dotnet publish "${SERVICE_PROJECT}" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
ARG SERVICE_DLL
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV SERVICE_DLL=${SERVICE_DLL}

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 CMD curl --fail http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["sh", "-c", "dotnet ${SERVICE_DLL}"]