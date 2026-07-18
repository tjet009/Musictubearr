# syntax=docker/dockerfile:1
# MusicTubearr — all-in-one image (app + UI + yt-dlp + ffmpeg)
# Build:  docker compose -f docker/docker-compose.yml up -d --build
# Open:   http://localhost:8686

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && npm install -g yarn \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

RUN dotnet publish src/NzbDrone.Console/Lidarr.Console.csproj \
      -c Release \
      -o /app/publish \
      -p:EnableAnalyzers=false \
      -p:SelfContained=false \
      -p:UseAppHost=false

RUN yarn install --frozen-lockfile --network-timeout 120000 \
    && yarn build --env production \
    && cp -a _output/UI /app/publish/UI

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime

RUN apt-get update && apt-get install -y --no-install-recommends \
        ffmpeg \
        python3 \
        python3-pip \
        curl \
        ca-certificates \
    && pip3 install --break-system-packages --no-cache-dir -U yt-dlp \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish/ /app/
COPY docker/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh \
    && mkdir -p /config /music /downloads /cookies

ENV XDG_CONFIG_HOME=/config
EXPOSE 8686
VOLUME ["/config", "/music", "/downloads", "/cookies"]
ENTRYPOINT ["/entrypoint.sh"]
