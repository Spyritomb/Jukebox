using Jukebox.Shared.Models;

namespace Jukebox.API.Interfaces
{
    public interface ISongLibraryService
    {
        public Song? GetById(int id);

        public string? GetFilePath(int id);

        public List<Song> LoadSongs();

        public IReadOnlyList<Song> GetAll();
    }
}
