FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ARG APP_UID=64198
ARG APP_GID=64198
ARG APP_USER=appuser
ARG APP_GROUP=appgroup
RUN set -eux; \
    if ! getent group ${APP_GID} >/dev/null; then groupadd -g ${APP_GID} ${APP_GROUP}; fi; \
    if ! id -u ${APP_UID} >/dev/null 2>&1; then useradd -m -u ${APP_UID} -g ${APP_GID} -s /bin/bash ${APP_USER}; fi
USER ${APP_USER}
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MapMe/MapMe/MapMe.csproj", "MapMe/MapMe/"]
COPY ["MapMe/MapMe.Client/MapMe.Client.csproj", "MapMe/MapMe.Client/"]
RUN dotnet restore "MapMe/MapMe/MapMe.csproj"
# Copy only necessary source files to avoid sensitive data
COPY ["MapMe/", "MapMe/"]
COPY ["global.json", "./"]
WORKDIR "/src/MapMe/MapMe"
RUN dotnet build "./MapMe.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MapMe.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG APP_UID=64198
ARG APP_GID=64198
ARG APP_USER=appuser
ARG APP_GROUP=appgroup
WORKDIR /app
# Copy published files and ensure proper ownership with read-only permissions
COPY --from=publish --chown=${APP_UID}:${APP_GID} --chmod=444 /app/publish .
# Explicitly switch to non-root user for security
USER ${APP_USER}
ENTRYPOINT ["dotnet", "MapMe.dll"]
