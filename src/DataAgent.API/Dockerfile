# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["DataAgent.sln", "./"]
COPY ["src/DataAgent.API/DataAgent.API.csproj", "src/DataAgent.API/"]
COPY ["src/DataAgent.Application/DataAgent.Application.csproj", "src/DataAgent.Application/"]
COPY ["src/DataAgent.Domain/DataAgent.Domain.csproj", "src/DataAgent.Domain/"]
COPY ["src/DataAgent.Infrastructure/DataAgent.Infrastructure.csproj", "src/DataAgent.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/DataAgent.API/DataAgent.API.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish
WORKDIR "/src/src/DataAgent.API"
RUN dotnet publish "DataAgent.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Install curl for healthcheck before creating user
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser:appuser /app
USER appuser

# Copy published files
COPY --from=build --chown=appuser:appuser /app/publish .

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DataAgent.API.dll"]
