using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace voks.client.model;

public partial class RecentChats : ObservableObject
{
    [ObservableProperty] private ObservableCollection<RecentChatItem>? _recentChatItems;
}
