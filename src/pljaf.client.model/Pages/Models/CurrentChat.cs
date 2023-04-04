using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace pljaf.client.model;

public partial class CurrentChat : ObservableObject
{
    [ObservableProperty] private string? name;
    [ObservableProperty] private string? topic;
    [ObservableProperty] private ObservableCollection<User>? members;
    [ObservableProperty] private ObservableCollection<Invite>? invites;
    [ObservableProperty] private ObservableCollection<Message>? messages;
    [ObservableProperty] private ConversationState? conversationState;
}
