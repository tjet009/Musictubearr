# MusicTubearr

<p align="center">
  <img src="Logo/256.png" alt="MusicTubearr" width="128" />
</p>

**YouTube-first music manager** — search artists, monitor releases, download with **yt-dlp**, convert with **ffmpeg**, and import into your library.

Forked from [Lidarr](https://github.com/Lidarr/Lidarr), rebuilt around YouTube instead of Usenet/BitTorrent and MusicBrainz.

**Version:** `0.1.0`

## Why MusicTubearr?

Lidarr is built for torrent/Usenet indexers and MusicBrainz metadata. MusicTubearr keeps the familiar *arr UI and automation, but:

- **YouTube is the catalog** (channels → artists, playlists/uploads → albums, videos → tracks)
- **yt-dlp + ffmpeg** handle download and conversion (MP3, AAC, ALAC, FLAC, …)
- **Cookies, SponsorBlock, Shorts filtering**, and music/music-video preference are first-class settings

## Major features

* YouTube search & library management in an *arr-style UI
* Built-in YouTube indexer + yt-dlp download client (no separate *arr download client required)
* Monitor artists and grab missing / interactive search releases
* Audio conversion: MP3, AAC / M4A, ALAC (iTunes), FLAC, Opus, WAV
* SponsorBlock trimming for intros, outros, sponsors, and non-music segments
* Exclude YouTube Shorts; prefer music / music videos
* Easy YouTube cookies: upload Netscape `cookies.txt`, paste a Cookie header, or Docker drop-in
* Docker image with yt-dlp + ffmpeg included
* Library scanning, renaming, quality upgrades, and connect notifications (Plex/Kodi/etc.) where still useful

## Quick start (Docker)

```powershell
git clone https://github.com/tjet009/Musictubearr.git
cd Musictubearr

# Edit docker/docker-compose.yml if your library path isn't C:\docker\media\youtube-music
docker compose -f docker/docker-compose.yml up -d --build
```

Open **http://localhost:8585**

1. Add Root Folder → `/music`
2. **Settings → Metadata** → add YouTube cookies (Upload / Paste / Import)
3. Optionally set **Audio Format** (e.g. AAC or ALAC for iTunes) and confirm **Exclude Shorts** / **Music only**
4. **Add New** → search a YouTube artist → Interactive Search → Grab

### YouTube cookies

Most YouTube downloads need auth cookies:

1. Private/incognito window → sign in to YouTube  
2. Export with [Get cookies.txt LOCALLY](https://chromewebstore.google.com/detail/get-cookiestxt-locally/cclelndahbckbenkjhflpdbgdldlbecc) (Chrome), [cookies.txt](https://addons.mozilla.org/en-US/firefox/addon/cookies-txt/) (Firefox), or [Cookie-Editor](https://chromewebstore.google.com/detail/cookie-editor/hlkenndednhfkekhgcdicdfddnkalmdm)  
3. Close the private window  
4. In MusicTubearr: **Upload**, **Paste**, or drop the file at `docker/cookies/cookies.txt` and click **Import** → **Test**

## Concepts

| Lidarr | MusicTubearr |
|---|---|
| Artist | YouTube channel |
| Album | Playlist / Uploads |
| Track | Video |
| Indexer | Built-in YouTube |
| Download client | Built-in yt-dlp |
| MusicBrainz / SkyHook | YouTube / yt-dlp |

## Settings highlights

| Setting | Where | Notes |
|---|---|---|
| Cookies | Settings → Metadata | Upload, paste, or `/cookies/cookies.txt` |
| Audio format | Settings → Metadata | AAC / ALAC for iTunes |
| SponsorBlock | Settings → Metadata | Trim intro/outro/sponsors (default: music preset) |
| Exclude Shorts | Settings → Metadata | On by default |
| Music only | Settings → Metadata | Prefer official audio / music videos |

## Native install (optional)

1. Build with .NET 8 + yarn (see [CONTRIBUTING.md](CONTRIBUTING.md))
2. Start MusicTubearr (default UI port **8585**)
3. Settings → Metadata → **Download** yt-dlp / ffmpeg if needed (Docker already includes them)

## Support

GitHub Issues are for bugs and feature requests for **this fork**:

* [Issues](https://github.com/tjet009/Musictubearr/issues)
* [Repository](https://github.com/tjet009/Musictubearr)

Upstream Lidarr docs and Discord do **not** support MusicTubearr-specific YouTube/yt-dlp behavior.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). PRs that improve YouTube discovery, download reliability, cookies UX, and library import are especially welcome.

## Upstream

MusicTubearr is derived from [Lidarr](https://github.com/Lidarr/Lidarr) (GPL-3.0). Thanks to the Lidarr / Servarr contributors for the foundation.

## License

* [GNU GPL v3](LICENSE.md)
* Copyright for modifications: MusicTubearr contributors
* Copyright for original Lidarr code: Lidarr / Servarr contributors
