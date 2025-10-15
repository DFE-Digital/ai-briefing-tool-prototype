# Set the major version of dotnet
ARG DOTNET_VERSION=9.0
# Set the major version of nodejs
ARG NODEJS_VERSION_MAJOR=22

# ==============================================
# Assets Build Stage (Node.js)
# ==============================================
FROM node:${NODEJS_VERSION_MAJOR}-bullseye-slim AS assets
WORKDIR /app
COPY ./src/wwwroot .
RUN npm ci --ignore-scripts && npm run build

# ==============================================
# .NET SDK Build Stage
# ==============================================
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-azurelinux3.0 AS build
WORKDIR /build

# Copy solution and props files
COPY ./src/BriefingTool.sln ./src/

## START: Restore Packages
ARG PROJECT_NAME="BriefingTool"
# Copy csproj files for restore caching
COPY ./src/${PROJECT_NAME}.csproj                         ./src/

# Mount GitHub Token and restore
 RUN dotnet restore ./src/${PROJECT_NAME}.sln
## END: Restore Packages

COPY ./src/ /build/src/
# Build and publish
RUN dotnet build ./src/${PROJECT_NAME}.sln -c Release

# Copy entrypoint script
COPY ./scripts/docker-entrypoint.sh /app/docker-entrypoint.sh

# ==============================================
# .NET: Runtime
# ==============================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS final
WORKDIR /app
LABEL org.opencontainers.image.source="https://github.com/DFE-Digital/ai-briefing-tool-prototype"

COPY --from=build /app .
COPY --from=assets /app ./wwwroot

# Set permissions and user
RUN chmod +x ./docker-entrypoint.sh
USER $APP_UID
