# 🎵 Jukebox

A .NET 8 Web API + Blazor WebAssembly jukebox app. Drop local audio files in, hit play.

## Solution Structure

```
Jukebox.sln
├── Jukebox.Shared      — Song model (shared between API + Client)
├── Jukebox.API         — .NET Core Web API (serves song list + audio streams)
└── Jukebox.Client      — Blazor WebAssembly SPA (jukebox UI)
```

## Quick Start

### 1. Add your music

Drop audio files into `Jukebox.API/Music/`.

Supported formats: `.mp3`, `.wav`, `.ogg`, `.flac`, `.aac`, `.m4a`

**Optional naming convention** — the library auto-parses filenames:
```
Artist - Title.mp3          →  Artist: "Artist",  Title: "Title"
SomeSong.mp3                →  Artist: "Unknown Artist", Title: "SomeSong"
```

### 2. Run the API

```bash
cd Jukebox.API
dotnet run
# Listening on https://localhost:7001
```

### 3. Run the Blazor client

```bash
cd Jukebox.Client
dotnet run
# Open https://localhost:7002
```

Both projects can be run simultaneously from Visual Studio by setting multiple startup projects.

---

## API Endpoints

| Method | Route                    | Description                          |
|--------|--------------------------|--------------------------------------|
| GET    | `/api/songs`             | Returns full song list               |
| GET    | `/api/songs/{id}`        | Returns a single song by ID          |
| GET    | `/api/songs/{id}/stream` | Streams the audio file (range-aware) |

The stream endpoint supports HTTP range requests, so the browser can seek freely.

---

## Features

- ▶ Play / ⏸ Pause / ⏹ Stop
- ⏮ Previous track (restarts current if >3 seconds in)
- ⏭ Skip to next
- ↺ Repeat current track
- ⇄ Shuffle mode
- Progress bar with click-to-seek
- Volume control
- Spinning disc animation while playing
- Auto-advance to next track on song end
- Song queue — click any track to play immediately

---

## Configuration

The music folder path is configurable in `Jukebox.API/appsettings.json`:

```json
{
  "MusicFolder": "Music"
}
```

Use an absolute path to point at any folder on disk:
```json
{
  "MusicFolder": "C:\\Users\\You\\Music"
}
```

The API base URL for the Blazor client is set in `Jukebox.Client/Program.cs`:
```csharp
BaseAddress = new Uri("https://localhost:7001")
```

Change this if you deploy the API elsewhere.

---

## Architecture Notes

- Audio playback is handled entirely by the browser's native `<audio>` element.
- Blazor communicates with it via JS interop (`wwwroot/js/player.js`).
- The `JukeboxPlayerService` is a scoped service holding all player state; components subscribe to its `StateChanged` event to re-render.
- The API uses `PhysicalFile(..., enableRangeProcessing: true)` so the browser can seek without downloading the whole file.


# Jukebox — Architecture Diagrams

## Solution Structure
```mermaid
graph TD
    Shared["Jukebox.Shared\nSong, ResponseObject models"]

    API["Jukebox.API\nSongsController, SongLibraryService"]
    IService["ISongLibraryService\nGetAll, GetById, GetFilePath"]
    Music["Music/ folder\nlocal audio files"]

    Client["Jukebox.Client\nBlazor WASM, JukeboxPage"]
    PlayerService["JukeboxPlayerService\nPlay, Stop, Skip, Queue state"]
    PlayerJS["player.js\nJS interop — audio element"]

    Shared --> API
    Shared --> Client
    API --> IService
    IService --> Music
    Client --> PlayerService
    PlayerService --> PlayerJS
    Client -- "HTTP (api/songs)" --> API
```

---

## Flow 1 — App load: fetch song list
```mermaid
sequenceDiagram
    participant UI as Blazor UI
    participant API as Jukebox.API
    participant Lib as SongLibraryService

    UI->>API: GET /api/songs
    API->>Lib: GetAll()
    Lib-->>API: IReadOnlyList<Song>
    API-->>UI: ResponseObject<List<Song>>
    UI->>UI: Player.LoadQueue(songs)
```

---

## Flow 2 — User plays a song
```mermaid
sequenceDiagram
    participant UI as Blazor UI
    participant PS as PlayerService
    participant JS as player.js
    participant API as Jukebox.API

    UI->>PS: PlayAsync(index)
    PS->>JS: setSrc(streamUrl)
    JS->>JS: audio.src = url
    JS->>JS: audio.load()
    JS->>JS: audio.play()
    JS->>API: GET /api/songs/{id}/stream
    API-->>JS: audio stream (range-aware)
```

---

## Flow 3 — Song ends: auto-advance
```mermaid
sequenceDiagram
    participant JS as player.js
    participant UI as JukeboxPage
    participant PS as PlayerService

    JS->>UI: OnSongEndedAsync() [JSInvokable]
    UI->>PS: OnSongEndedAsync()
    alt Repeat on
        PS->>JS: setSrc(same url)
    else Shuffle on
        PS->>PS: random next index
        PS->>JS: setSrc(new url)
    else Normal
        PS->>PS: currentIndex + 1
        PS->>JS: setSrc(next url)
    end
```

---

## Flow 4 — Stop / Pause / Seek
```mermaid
sequenceDiagram
    participant UI as Blazor UI
    participant PS as PlayerService
    participant JS as player.js

    UI->>PS: StopAsync()
    PS->>JS: stop()
    JS->>JS: audio.pause() + currentTime = 0

    UI->>PS: TogglePlayPauseAsync()
    PS->>JS: pause() or resume()

    UI->>PS: SeekAsync(seconds)
    PS->>JS: seek(seconds)
    JS->>JS: audio.currentTime = seconds
```

---

## Flow 5 — Volume change
```mermaid
sequenceDiagram
    participant UI as Blazor UI
    participant PS as PlayerService
    participant JS as player.js

    UI->>PS: SetVolumeAsync(0.8)
    PS->>PS: Volume = Clamp(0.8, 0, 1)
    PS->>JS: setVolume(0.8)
    JS->>JS: audio.volume = 0.8
```
Model used:
Claude: Sonnet 4.6
