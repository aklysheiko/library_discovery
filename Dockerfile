# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files for layer caching
COPY library_discovery.sln .
COPY LibraryDiscovery/LibraryDiscovery.csproj LibraryDiscovery/
COPY src/LibraryDiscovery.Application/LibraryDiscovery.Application.csproj src/LibraryDiscovery.Application/
COPY src/LibraryDiscovery.Domain/LibraryDiscovery.Domain.csproj src/LibraryDiscovery.Domain/
COPY src/LibraryDiscovery.Infrastructure/LibraryDiscovery.Infrastructure.csproj src/LibraryDiscovery.Infrastructure/

RUN dotnet restore LibraryDiscovery/LibraryDiscovery.csproj

# Copy source and publish
COPY LibraryDiscovery/ LibraryDiscovery/
COPY src/ src/
RUN dotnet publish LibraryDiscovery/LibraryDiscovery.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "LibraryDiscovery.dll"]
