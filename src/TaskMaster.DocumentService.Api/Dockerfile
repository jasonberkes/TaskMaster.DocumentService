# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG FEED_ACCESSTOKEN
WORKDIR /src

# Copy nuget.config first
COPY ["nuget.config", "./"]

# Add credentials directly to nuget.config using the token
RUN dotnet nuget update source tm-packages \
    --username "docker" \
    --password "${FEED_ACCESSTOKEN}" \
    --store-password-in-clear-text \
    --configfile nuget.config

# Copy solution and project files
COPY ["TaskMaster.DocumentService.sln", "./"]
COPY ["src/TaskMaster.DocumentService.Api/TaskMaster.DocumentService.Api.csproj", "src/TaskMaster.DocumentService.Api/"]
COPY ["src/TaskMaster.DocumentService.Core/TaskMaster.DocumentService.Core.csproj", "src/TaskMaster.DocumentService.Core/"]
COPY ["src/TaskMaster.DocumentService.Data/TaskMaster.DocumentService.Data.csproj", "src/TaskMaster.DocumentService.Data/"]
COPY ["src/TaskMaster.DocumentService.Search/TaskMaster.DocumentService.Search.csproj", "src/TaskMaster.DocumentService.Search/"]
COPY ["src/TaskMaster.DocumentService.Processing/TaskMaster.DocumentService.Processing.csproj", "src/TaskMaster.DocumentService.Processing/"]
COPY ["src/TaskMaster.DocumentService.SDK/TaskMaster.DocumentService.SDK.csproj", "src/TaskMaster.DocumentService.SDK/"]

# Restore dependencies
RUN dotnet restore "src/TaskMaster.DocumentService.Api/TaskMaster.DocumentService.Api.csproj"

# Copy source code
COPY ["src/", "src/"]

# Build and publish
RUN dotnet build "src/TaskMaster.DocumentService.Api/TaskMaster.DocumentService.Api.csproj" -c Release -o /app/build --no-restore
RUN dotnet publish "src/TaskMaster.DocumentService.Api/TaskMaster.DocumentService.Api.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TaskMaster.DocumentService.Api.dll"]
