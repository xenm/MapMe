FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MapMe/MapMe/MapMe.csproj", "MapMe/MapMe/"]
COPY ["MapMe/MapMe.Client/MapMe.Client.csproj", "MapMe/MapMe.Client/"]
RUN dotnet restore "MapMe/MapMe/MapMe.csproj"
COPY . .
WORKDIR "/src/MapMe/MapMe"
RUN dotnet build "./MapMe.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MapMe.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MapMe.dll"]
