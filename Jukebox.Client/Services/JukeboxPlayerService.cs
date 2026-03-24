using Jukebox.Shared.Models;
using Microsoft.JSInterop;

namespace Jukebox.Client.Services;

public class JukeboxPlayerService
{
    private readonly IJSRuntime _js;

    public List<Song> Queue { get; private set; } = new();
    public int CurrentIndex { get; private set; } = -1;
    public bool IsPlaying { get; private set; }
    public bool IsShuffle { get; private set; }
    public bool IsRepeat { get; private set; }
    public double CurrentTime { get; private set; }
    public double Duration { get; private set; }
    public double Volume { get; private set; } = 1.0;

    public Song? CurrentSong => CurrentIndex >= 0 && CurrentIndex < Queue.Count
        ? Queue[CurrentIndex] : null;

    public event Action? StateChanged;

    // Set by the page so the service can resolve stream URLs without knowing the base address
    public Func<int, string>? UrlResolver { get; set; }

    public JukeboxPlayerService(IJSRuntime js) => _js = js;

    public void LoadQueue(IEnumerable<Song> songs)
    {
        Queue = songs.ToList();
        CurrentIndex = -1; // nothing selected until user acts
        IsPlaying = false;
        NotifyState();
    }

    public async Task PlayAsync(int index)
    {
        if (index < 0 || index >= Queue.Count) return;
        CurrentIndex = index;
        CurrentTime = 0;
        Duration = 0;
        IsPlaying = true;
        NotifyState();
        var url = UrlResolver?.Invoke(Queue[index].Id) ?? string.Empty;
        await _js.InvokeVoidAsync("jukeboxPlayer.setSrc", url);
    }

    public async Task TogglePlayPauseAsync()
    {
        if (CurrentSong is null) return;
        IsPlaying = !IsPlaying;
        NotifyState();
        if (IsPlaying)
            await _js.InvokeVoidAsync("jukeboxPlayer.resume");
        else
            await _js.InvokeVoidAsync("jukeboxPlayer.pause");
    }

    public async Task StopAsync()
    {
        IsPlaying = false;
        CurrentTime = 0;
        NotifyState();
        await _js.InvokeVoidAsync("jukeboxPlayer.stop");
    }

    public async Task NextAsync()
    {
        if (Queue.Count == 0) return;
        int next = IsShuffle
            ? new Random().Next(Queue.Count)
            : (CurrentIndex + 1) % Queue.Count;
        await PlayAsync(next);
    }

    public async Task PrevAsync()
    {
        if (Queue.Count == 0) return;
        if (CurrentTime > 3)
        {
            await _js.InvokeVoidAsync("jukeboxPlayer.seek", 0);
            return;
        }
        int prev = CurrentIndex <= 0 ? Queue.Count - 1 : CurrentIndex - 1;
        await PlayAsync(prev);
    }

    public async Task SeekAsync(double seconds)
    {
        await _js.InvokeVoidAsync("jukeboxPlayer.seek", seconds);
    }

    public async Task SetVolumeAsync(double volume)
    {
        Volume = Math.Clamp(volume, 0, 1);
        NotifyState();
        await _js.InvokeVoidAsync("jukeboxPlayer.setVolume", Volume);
    }

    public void ToggleShuffle() { IsShuffle = !IsShuffle; NotifyState(); }
    public void ToggleRepeat() { IsRepeat = !IsRepeat; NotifyState(); }

    [JSInvokable]
    public async Task OnSongEndedAsync()
    {
        if (IsRepeat && CurrentSong is not null)
        {
            var url = UrlResolver?.Invoke(CurrentSong.Id) ?? string.Empty;
            await _js.InvokeVoidAsync("jukeboxPlayer.setSrc", url);
            return;
        }
        await NextAsync();
    }

    [JSInvokable]
    public Task OnTimeUpdateAsync(double current, double duration)
    {
        CurrentTime = current;
        Duration = duration;
        NotifyState();
        return Task.CompletedTask;
    }

    private void NotifyState() => StateChanged?.Invoke();
}
