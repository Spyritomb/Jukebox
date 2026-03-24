using Jukebox.API.Interfaces;
using Jukebox.API.Services;
using Jukebox.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace Jukebox.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SongsController : ControllerBase
{
    private readonly ISongLibraryService _library;

    public SongsController(ISongLibraryService library)
    {
        _library = library;
    }

    // GET /api/songs
    [HttpGet]
    public ActionResult<ResponseObject<IEnumerable<Song>>> GetAll()
    {
        try
        {
            return Ok(new ResponseObject<IEnumerable<Song>>
            {
                dto = _library.GetAll(),
                IsError = false
            });
        }
        catch (Exception ex)
        {
            return Ok(new ResponseObject<IEnumerable<Song>>
            {
                IsError = true,
                ErrorMessage = ex.Message
            });
        }
    }

    // GET /api/songs/{id}
    [HttpGet("{id:int}")]
    public ActionResult<Song> GetById(int id)
    {
        var song = _library.GetById(id);
        return song is null ? NotFound() : Ok(song);
    }

    // GET /api/songs/{id}/stream  — supports HTTP range requests for seeking
    [HttpGet("{id:int}/stream")]
    public IActionResult Stream(int id)
    {
        var path = _library.GetFilePath(id);
        if (path is null) return NotFound();

        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".flac" => "audio/flac",
            ".aac" => "audio/aac",
            ".m4a" => "audio/mp4",
            _ => "application/octet-stream"
        };

        // PhysicalFile with EnableRangeProcessing lets the browser seek
        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }
}
