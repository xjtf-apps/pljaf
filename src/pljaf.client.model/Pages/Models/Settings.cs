using CommunityToolkit.Mvvm.ComponentModel;

namespace pljaf.client.model;

public partial class Settings : ObservableObject
{
    [ObservableProperty] private bool? getNotifications;
}