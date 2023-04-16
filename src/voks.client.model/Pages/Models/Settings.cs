using CommunityToolkit.Mvvm.ComponentModel;

namespace voks.client.model;

public partial class Settings : ObservableObject
{
    [ObservableProperty] private bool? getNotifications;
}