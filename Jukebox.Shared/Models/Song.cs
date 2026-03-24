using System;

namespace Jukebox.Shared.Models;

public class Song
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string Album { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string FileName { get; set; } = string.Empty;

    public string DurationFormatted =>
        TimeSpan.FromSeconds(DurationSeconds).ToString(DurationSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");
}
