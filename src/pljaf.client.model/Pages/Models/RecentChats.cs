using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace pljaf.client.model;

public partial class RecentChats : ObservableObject
{
    [ObservableProperty] private ObservableCollection<RecentChatItem>? _recentChats;
}
