# MusicTubearr

YouTube-first music manager (*arr UI) powered by **yt-dlp** + **ffmpeg**.

Search YouTube artists → browse albums/playlists → download & convert → import into your library.

## Easiest: Docker (recommended)

No .NET, yarn, yt-dlp, or ffmpeg install on the host. Everything is in the image.

```powershell
git clone https://github.com/tjet009/Musictubearr.git
cd Musictubearr
git checkout cursor/youtube-first-musictubearr-bb21

# Edit docker/docker-compose.yml if your library path isn't C:\docker\media\youtube-music
docker compose -f docker/docker-compose.yml up -d --build
```

Open **http://localhost:8585**

1. Add Root Folder → `/music` (maps to your host library folder)
2. Settings → Metadata → **Upload** your YouTube `cookies.txt`
3. (Optional) Click **Download** next to yt-dlp/ffmpeg — already included in Docker, but available for native installs
4. Add New → search a YouTube artist → Interactive Search → Grab

### Cookie export (needed for most YouTube downloads)

1. Private/incognito window → log into YouTube  
2. Export `youtube.com` cookies as Netscape `cookies.txt`  
3. Close the private window  
4. Upload in Settings → Metadata (or put the file in `docker/cookies/cookies.txt`)

## Native install (optional)

If you prefer not to use Docker:

1. Build with .NET 8 + yarn (see CONTRIBUTING.md)
2. Start MusicTubearr
3. Settings → Metadata → click **Download** for yt-dlp and ffmpeg (Windows auto-fetches both)

On first launch MusicTubearr also tries to auto-download yt-dlp into its AppData `tools` folder.

## Architecture

| Lidarr concept | MusicTubearr |
|---|---|
| Artist | YouTube channel |
| Album | Playlist / Uploads |
| Track | Video |
| Indexer | Built-in YouTube |
| Download client | Built-in yt-dlp |

## License

GPL-3.0 (from MusicTubearr). See [LICENSE.md](LICENSE.md).
