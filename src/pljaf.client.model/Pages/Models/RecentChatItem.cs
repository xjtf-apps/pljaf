using CommunityToolkit.Mvvm.ComponentModel;

namespace pljaf.client.model;

public partial class RecentChatItem : ObservableObject
{
    [ObservableProperty] private ConvId? convId;
    [ObservableProperty] private Uri? iconUri;
    [ObservableProperty] private string? name;
    [ObservableProperty] private string? topic;
    [ObservableProperty] private string? mostRecentMessagePreview;
    [ObservableProperty] private ConversationState? conversationState;
    [ObservableProperty] private bool unread;
}
