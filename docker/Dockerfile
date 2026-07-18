# MusicTubearr runtime image based on .NET 8 + yt-dlp + ffmpeg
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
        ffmpeg \
        python3 \
        python3-pip \
        curl \
        ca-certificates \
    && pip3 install --break-system-packages --no-cache-dir -U yt-dlp \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY out/ /app/

ENV XDG_CONFIG_HOME=/config
EXPOSE 8686

VOLUME ["/config", "/music", "/downloads", "/cookies"]

ENTRYPOINT ["dotnet", "Lidarr.dll", "-nobrowser", "-data=/config"]
