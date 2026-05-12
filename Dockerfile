# Set the major version of dotnet
ARG DOTNET_VERSION=10.0
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
WORKDIR /build/src

ARG PROJECT_NAME="BriefingTool"

# Copy everything at once
COPY ./src/ .

# Restore, build and publish in sequence
RUN dotnet restore ${PROJECT_NAME}.sln && \
    dotnet build ${PROJECT_NAME}.sln -c Release --no-restore && \
    dotnet publish ${PROJECT_NAME}.sln --no-build -c Release -o /app

# ==============================================
# .NET: Runtime
# ==============================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS final
WORKDIR /app
LABEL org.opencontainers.image.source="https://github.com/DFE-Digital/ai-briefing-tool-prototype"
LABEL org.opencontainers.image.description="Briefing Tool is a web application that provides a user-friendly interface for generating and managing AI-generated briefings."

COPY --from=build /app .
COPY --from=assets /app ./wwwroot

# Copy entrypoint script, fix line endings and set permissions
COPY ./scripts/docker-entrypoint.sh ./docker-entrypoint.sh
RUN sed -i 's/\r//' ./docker-entrypoint.sh && \
    chmod +x ./docker-entrypoint.sh

USER $APP_UID

ENTRYPOINT ["./docker-entrypoint.sh"]
CMD ["dotnet", "BriefingTool.dll"]