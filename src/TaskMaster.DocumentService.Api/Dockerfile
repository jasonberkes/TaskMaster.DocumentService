# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

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
WORKDIR "/src/src/TaskMaster.DocumentService.Api"
RUN dotnet build "TaskMaster.DocumentService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "TaskMaster.DocumentService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published files
COPY --from=build /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set ASP.NET Core to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "TaskMaster.DocumentService.Api.dll"]
