FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ["libraries/src/Nexus.Auth/Nexus.Auth.csproj", "libraries/src/Nexus.Auth/"]
COPY ["libraries/src/Nexus.Configuration/Nexus.Configuration.csproj", "libraries/src/Nexus.Configuration/"]
COPY ["libraries/src/Nexus.Core/Nexus.Core.csproj", "libraries/src/Nexus.Core/"]
COPY ["libraries/src/Nexus.Logging/Nexus.Logging.csproj", "libraries/src/Nexus.Logging/"]
COPY ["libraries/src/Nexus.Persistence/Nexus.Persistence.csproj", "libraries/src/Nexus.Persistence/"]
COPY ["libraries/src/Nexus.Framework.Web/Nexus.Framework.Web.csproj", "libraries/src/Nexus.Framework.Web/"]
COPY ["libraries/src/Nexus.Management/Nexus.Management.csproj", "libraries/src/Nexus.Management/"]
COPY ["libraries/src/Nexus.Telemetry/Nexus.Telemetry.csproj", "libraries/src/Nexus.Telemetry/"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]

COPY ["services/company-api/src/Nexus.Company.Api/nuget.config", "services/company-api/src/Nexus.Company.Api/"]
COPY ["services/company-api/src/Nexus.Company.Api/Nexus.Company.Api.csproj", "services/company-api/src/Nexus.Company.Api/"]

RUN dotnet restore "services/company-api/src/Nexus.Company.Api/Nexus.Company.Api.csproj"

COPY . .

WORKDIR "/src/services/company-api/src/Nexus.Company.Api"
RUN dotnet build "Nexus.Company.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nexus.Company.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.Company.Api.dll"]
