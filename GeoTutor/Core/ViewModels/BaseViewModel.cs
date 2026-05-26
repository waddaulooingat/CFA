namespace GeoTutor.Core.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    protected void SetBusy(bool busy, string message = "")
    {
        IsBusy = busy;
        StatusMessage = message;
    }
}
