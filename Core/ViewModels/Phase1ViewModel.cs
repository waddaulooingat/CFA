namespace GeoTutor.Core.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoTutor.Data;
using GeoTutor.SceneEngine.Models;
using System.Collections.Generic;

/// <summary>
/// Phase 1 view-model: cycles through hard-coded sample scenes.
/// No LLM, no TTS, no database queries beyond schema creation.
/// </summary>
public partial class Phase1ViewModel : ObservableObject
{
    private readonly List<(string Title, SceneSpec Scene)> _scenes = SampleScenes.All;
    private int _index;

    [ObservableProperty] private string _sceneTitle  = "";
    [ObservableProperty] private SceneSpec? _currentScene;
    [ObservableProperty] private string _sceneCounter = "";

    public Phase1ViewModel()
    {
        ShowScene(0);
    }

    [RelayCommand]
    private void Next()
    {
        ShowScene((_index + 1) % _scenes.Count);
    }

    [RelayCommand]
    private void Previous()
    {
        ShowScene((_index - 1 + _scenes.Count) % _scenes.Count);
    }

    private void ShowScene(int index)
    {
        _index        = index;
        SceneTitle    = _scenes[index].Title;
        CurrentScene  = _scenes[index].Scene;
        SceneCounter  = $"{index + 1} / {_scenes.Count}";
    }
}
