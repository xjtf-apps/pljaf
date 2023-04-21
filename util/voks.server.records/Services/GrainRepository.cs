using Azure.Data.Tables;

using Microsoft.Extensions.Configuration;

using Orleans;

using voks.server.api;
using voks.server.model;

namespace voks.server.records
{
    public class GrainRepository
    {
        private readonly IGrainFactory GrainFactory;
        private readonly TableClient GrainStateClient;
        private const string GrainStateTableName = "OrleansGrainState";

        public GrainRepository(IConfiguration hostConfig, IGrainFactory grainFactory)
        {
            var actorSystemSection = hostConfig.GetRequiredSection("ActorSystem");
            var actorSystemConfig = actorSystemSection.Get<ActorSystemSettings>()!;

            var connectionString = actorSystemConfig.ClusteringConnectionString;
            var tableClientService = new TableServiceClient(connectionString);
            var tableClient = tableClientService.GetTableClient(GrainStateTableName);

            GrainFactory = grainFactory;
            GrainStateClient = tableClient;
            GrainStateClient.CreateIfNotExists();
        }

        public async Task<List<UserModel>> GetUsers()
        {
            var users = new List<UserModel>();
            var userIds = GrainStateClient
                .Query<GrainStoreTableEntity>(entity => true).ToList()
                .Where(entity => entity.PartitionKey.StartsWith("v1_service_user"))
                .Select(row => row.PartitionKey.Replace("v1_service_user_", "")).Distinct().ToList();

            var userGrains = userIds
                .Select(id => GrainFactory.GetGrain<IUserGrain>(id)).ToList();

            for (int i = 0; i < userGrains.Count; i++)
            {
                var userGrain = userGrains[i];
                var userProfile = await userGrain.GetProfileAsync();
                users.Add(new UserModel()
                {
                    Phone = userIds[i],
                    DisplayName = userProfile.DisplayName ?? "<No display name>",
                    StatusLine = userProfile.StatusLine ?? "<No status line>"
                });
            }
            return users;
        }

        public async Task<List<ConversationModel>> GetConversations()
        {
            var conversations = new List<ConversationModel>();
            var conversationGrains = GrainStateClient
                .Query<GrainStoreTableEntity>(entity => true).ToList()
                .Where(entity => entity.PartitionKey.StartsWith("v1_service_conversation"))
                .Select(row => row.PartitionKey.Replace("v1_service_conversation_", ""))
                .Select(Guid.Parse).Distinct()
                .Select(id => GrainFactory.GetGrain<IConversationGrain>(id)).ToList();

            for (int i = 0; i < conversationGrains.Count; i++)
            {
                var membersList = new List<string>();
                var messagesList = new List<MessageModel>();
                var members = await conversationGrains[i].GetMembersAsync();
                var messages = await conversationGrains[i].GetMessagesAsync();
                var convId = (await conversationGrains[i].GetIdAsync()).ToString();

                for (int j = 0; j < members.Count; j++)
                {
                    var memberGrain = members[j];
                    var memberId = await memberGrain.GetIdAsync();
                    membersList.Add(memberId.ToString());
                }

                for (int j = 0; j < messages.Count; j++)
                {
                    var messageGrain = messages[j];
                    var message = new MessageModel()
                    {
                        Conversation = convId,
                        Id = await messageGrain.GetIdAsync(),
                        Text = await messageGrain.GetEncryptedTextDataAsync(),
                        Timestamp = await messageGrain.GetTimestampAsync(),
                        Sender = await (await messageGrain.GetSenderAsync()).GetIdAsync()
                    };
                    messagesList.Add(message);
                }

                conversations.Add(new ConversationModel()
                {
                    Id = convId,
                    Members = membersList,
                    Messages = messagesList,
                    Name = await conversationGrains[i].GetNameAsync(),
                    Topic = await conversationGrains[i].GetTopicAsync(),
                    Kind = membersList.Count == 2 ? "OneOnOne" : "Group"
                });
            }
            return conversations;
        }

        public async Task Save(List<UserModel> users, List<ConversationModel> conversations, List<MessageModel> messages)
        {
            var userGrains = users.Select(u => GrainFactory.GetGrain<IUserGrain>(u.Phone)).ToList();
            var messageGrains = messages.Select(m => GrainFactory.GetGrain<IMessageGrain>(m.Id)).ToList();
            var conversationGrains = conversations.Select(c => GrainFactory.GetGrain<IConversationGrain>(Guid.Parse(c.Id))).ToList();

            for (int u = 0; u < userGrains.Count; u++)
            {
                var user = users[u];
                var userGrain = userGrains[u];
                var userProfile = new Profile()
                {
                    DisplayName = user.DisplayName,
                    StatusLine = user.StatusLine
                };
                await userGrain.SetProfileAsync(userProfile);
                await userGrain.SetTokensAsync(
                    new Tokens() { AccessToken = "", RefreshToken = "", RefreshTokenExpires = DateTime.UtcNow });

                var userCurrentConversations = await (await userGrain.GetConversationsAsync()).ToAsyncEnumerable()
                    .SelectAwait(async conv => await conv.GetIdAsync()).Select(cId => cId.ToString())
                    .ToListAsync();

                var newUserConversations = conversations
                    .Where(c => c.Members.Contains(user.Phone))
                    .Select(c => c.Id).Where(cId => !userCurrentConversations.Contains(cId))
                    .ToList();

                foreach (var newConversation in newUserConversations)
                {
                    var conversationId = Guid.Parse(newConversation);

                    var conversationGrain = GrainFactory.GetGrain<IConversationGrain>(conversationId);
                    await userGrain.Internal_AddToConversationAsync(conversationId);
                    await conversationGrain.EnterConversationAsync(userGrain);
                }
            }

            for (int c = 0; c < conversations.Count; c++)
            {
                var conversation = conversations[c];
                var conversationGrain = conversationGrains[c];

                var currentMessages = await (await conversationGrain.GetMessagesAsync()).ToAsyncEnumerable()
                    .SelectAwait(async m => await m.GetIdAsync())
                    .ToListAsync();

                var newMessages = messages
                    .Where(m => m.Conversation == conversation.Id)
                    .Where(m => !currentMessages.Contains(m.Id))
                    .Select(m => m.Id)
                    .ToList();

                foreach (var newMessage in newMessages)
                {
                    var message = messages.First(m => m.Id == newMessage);
                    var messageGrain = GrainFactory.GetGrain<IMessageGrain>(newMessage);

                    await messageGrain.AuthorMessageAsync(
                        sender: GrainFactory.GetGrain<IUserGrain>(message.Sender),
                        timestamp: message.Timestamp, encryptedTextData: message.Text, media: null);

                    await conversationGrain.PostMessageAsync(messageGrain);
                }
            }

            // TODO: remove removed state from store!
        }
    }
}
