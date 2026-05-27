namespace GeoTutor.Core.Services;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

/// <summary>
/// Wraps WPF MediaPlayer for fire-and-forget .mp3 playback on the UI thread.
/// </summary>
public class AudioPlayerService
{
    private MediaPlayer? _player;
    private readonly Dispatcher _dispatcher;

    public event Action? PlaybackEnded;

    public AudioPlayerService()
    {
        _dispatcher = Application.Current.Dispatcher;
    }

    public void Play(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        _dispatcher.InvokeAsync(() =>
        {
            _player?.Stop();
            _player?.Close();

            _player = new MediaPlayer();
            _player.MediaEnded += (_, _) => PlaybackEnded?.Invoke();
            _player.MediaFailed += (_, _) => PlaybackEnded?.Invoke();
            _player.Open(new Uri(filePath, UriKind.Absolute));
            _player.Play();
        });
    }

    public void Stop()
    {
        _dispatcher.InvokeAsync(() =>
        {
            _player?.Stop();
            _player?.Close();
            _player = null;
        });
    }
}
