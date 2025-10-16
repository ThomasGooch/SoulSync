# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy solution and project files
COPY SoulSyncDatingApp.sln .
COPY src/SoulSync.Core/SoulSync.Core.csproj src/SoulSync.Core/
COPY src/SoulSync.Data/SoulSync.Data.csproj src/SoulSync.Data/
COPY src/SoulSync.Agents/SoulSync.Agents.csproj src/SoulSync.Agents/
COPY src/SoulSync.Services/SoulSync.Services.csproj src/SoulSync.Services/
COPY src/SoulSync.Web/SoulSync.Web.csproj src/SoulSync.Web/
COPY tests/SoulSync.Core.Tests/SoulSync.Core.Tests.csproj tests/SoulSync.Core.Tests/
COPY tests/SoulSync.Agents.Tests/SoulSync.Agents.Tests.csproj tests/SoulSync.Agents.Tests/
COPY tests/SoulSync.Services.Tests/SoulSync.Services.Tests.csproj tests/SoulSync.Services.Tests/
COPY tests/SoulSync.Web.Tests/SoulSync.Web.Tests.csproj tests/SoulSync.Web.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source files
COPY . .

# Build and publish the application
WORKDIR /source/src/SoulSync.Web
RUN dotnet publish -c Release -o /app --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app .

# Create non-root user for security
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "SoulSync.Web.dll"]
