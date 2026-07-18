# MusicTubearr

YouTube-first music collection manager forked from [Lidarr](https://github.com/Lidarr/Lidarr). Same *arr UI and automation (library, Wanted, Interactive Search, Queue), but **YouTube + yt-dlp + ffmpeg** replace MusicBrainz/SkyHook metadata and Usenet/torrent indexers.

## Features

* Search YouTube artists/channels and add them like Lidarr artists
* Browse channel **Uploads** and **playlists** as albums/tracks
* Interactive Search to pick videos, or Automatic Search / Wanted / RSS-style channel polling
* Download with **yt-dlp**, convert with **ffmpeg** to MP3/M4A/FLAC/Opus/WAV
* Netscape **cookies.txt** auth (Stacher-style) under Settings → Metadata → YouTube / yt-dlp
* Import, rename, and organize into your music library (e.g. `C:\docker\media\youtube-music`)

## Requirements

* .NET 8 SDK (for building)
* [yt-dlp](https://github.com/yt-dlp/yt-dlp) on PATH (or configure path in settings)
* [ffmpeg](https://ffmpeg.org/) on PATH (or configure path in settings)
* Optional: YouTube `cookies.txt` for age-restricted / member content

## Quick start (Docker)

See [`docker/docker-compose.yml`](docker/docker-compose.yml).

```bash
# Example mounts for a Windows host library
# C:\docker\media\youtube-music -> /music
# Place cookies at ./cookies/cookies.txt
docker compose -f docker/docker-compose.yml up -d
```

Open `http://localhost:8686`, set Root Folder to `/music`, configure cookies/format under **Settings → Metadata**.

### Cookie export

1. Open a private/incognito browser window and log into YouTube  
2. Export `youtube.com` cookies in Netscape format  
3. Close the private window (avoids cookie rotation)  
4. Mount/upload as `cookies.txt` and set **Cookies File** in settings (or `POST /api/v1/config/youtube/cookies`)

## Architecture

| Lidarr concept | MusicTubearr |
|---|---|
| Artist | YouTube channel (`yt:channel:UC…`) |
| Album | Playlist or synthetic Uploads album |
| Track | YouTube video |
| Indexer | Built-in **YouTube** indexer |
| Download client | Built-in **yt-dlp** client |
| Metadata | **YouTubeProxy** via yt-dlp JSON (not SkyHook) |

On first start, MusicTubearr auto-creates the YouTube indexer and yt-dlp download client.

## Development

```bash
# Backend
./build.sh

# Frontend
yarn install
yarn start
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the Lidarr-based build workflow.

## License

GPL-3.0 (inherited from Lidarr). See [LICENSE.md](LICENSE.md).
