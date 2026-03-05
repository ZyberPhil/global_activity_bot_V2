# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

# Copy solution and project files first for layer caching
COPY GlobalActivityBot.sln ./
COPY src/GlobalActivityBot/GlobalActivityBot.csproj ./src/GlobalActivityBot/
COPY src/GlobalActivityBot.Tests/GlobalActivityBot.Tests.csproj ./src/GlobalActivityBot.Tests/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the source code
COPY src/ ./src/

# Build and publish
RUN dotnet publish src/GlobalActivityBot/GlobalActivityBot.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd --gid 1001 botuser && \
    useradd --uid 1001 --gid 1001 --no-create-home --shell /bin/false botuser

# Copy published app
COPY --from=build /app/publish .

# Set permissions
RUN chown -R botuser:botuser /app

# Switch to non-root user
USER botuser

# Health check - verify the process is running
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD pgrep -f GlobalActivityBot || exit 1

ENTRYPOINT ["./GlobalActivityBot"]
