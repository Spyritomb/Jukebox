using Jukebox.API.Interfaces;
using Jukebox.Shared.Models;

namespace Jukebox.API.Services;

public class SongLibraryService : ISongLibraryService
{
    private readonly List<Song> _songs;
    private readonly string _musicFolder;

    public SongLibraryService(IWebHostEnvironment env, IConfiguration config)
    {
        // Music folder: configurable, defaults to /Music inside content root
        var configured = config["MusicFolder"];
        _musicFolder = string.IsNullOrEmpty(configured)
            ? Path.Combine(env.ContentRootPath, "Music")
            : Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(env.ContentRootPath, configured);

        _songs = LoadSongs();
    }

    public IReadOnlyList<Song> GetAll() => _songs.AsReadOnly();

    public Song? GetById(int id) => _songs.FirstOrDefault(s => s.Id == id);

    public string? GetFilePath(int id)
    {
        var song = GetById(id);
        if (song is null) return null;
        var path = Path.Combine(_musicFolder, song.FileName);
        return File.Exists(path) ? path : null;
    }

    public List<Song> LoadSongs()
    {
        var songs = new List<Song>();

        if (!Directory.Exists(_musicFolder))
        {
            Directory.CreateDirectory(_musicFolder);
            return songs;
        }

        var supported = new[] { ".mp3", ".wav", ".ogg", ".flac", ".aac", ".m4a" };
        var files = Directory.GetFiles(_musicFolder)
            .Where(f => supported.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => f)
            .ToList();

        int id = 1;
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);

            // Try to parse "Artist - Title" convention, else use filename
            string title, artist;
            if (name.Contains(" - "))
            {
                var parts = name.Split(" - ", 2);
                artist = parts[0].Trim();
                title = parts[1].Trim();
            }
            else
            {
                title = name;
                artist = "Unknown Artist";
            }

            songs.Add(new Song
            {
                Id = id++,
                Title = title,
                Artist = artist,
                Album = "Local Library",
                FileName = Path.GetFileName(file),
                DurationSeconds = 0 // Duration resolved client-side via the audio element
            });
        }

        return songs;
    }
}
