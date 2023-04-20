using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace voks.server.records
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GrainRepository? _grainRepository;

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(UserModel), typeof(MainWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(MessageModel), typeof(MainWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty ConversationProperty =
            DependencyProperty.Register("Conversation", typeof(ConversationModel), typeof(MainWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty UsersProperty =
            DependencyProperty.Register("Users", typeof(ObservableCollection<UserModel>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<UserModel>()));

        public static readonly DependencyProperty MessagesProperty =
            DependencyProperty.Register("Messages", typeof(ObservableCollection<MessageModel>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<MessageModel>()));

        public static readonly DependencyProperty ConversationsProperty =
            DependencyProperty.Register("Conversations", typeof(ObservableCollection<ConversationModel>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<ConversationModel>()));

        #region debug
        public static readonly DependencyPropertyKey IsDebugPropertyKey =
            DependencyProperty.RegisterReadOnly("IsDebug", typeof(bool), typeof(MainWindow), new PropertyMetadata(AssemblyVersionProvider.IsDebugAssembly()));

        public static readonly DependencyProperty IsDebugProperty =
            IsDebugPropertyKey.DependencyProperty;

        public bool IsDebug
        {
            get => (bool)GetValue(IsDebugProperty);
            set => SetValue(IsDebugProperty, value);
        }
        #endregion

        public UserModel User
        {
            get => (UserModel)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public MessageModel Message
        {
            get => (MessageModel)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public ConversationModel Conversation
        {
            get => (ConversationModel)GetValue(ConversationProperty);
            set => SetValue(ConversationProperty, value);
        }

        public ObservableCollection<UserModel> Users
        {
            get => (ObservableCollection<UserModel>)GetValue(UsersProperty);
            set => SetValue(UsersProperty, value);
        }

        public ObservableCollection<MessageModel> Messages
        {
            get => (ObservableCollection<MessageModel>)GetValue(MessagesProperty);
            set => SetValue(MessagesProperty, value);
        }

        public ObservableCollection<ConversationModel> Conversations
        {
            get => (ObservableCollection<ConversationModel>)GetValue(ConversationsProperty);
            set => SetValue(ConversationsProperty, value);
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _grainRepository = serviceProvider.GetRequiredService<GrainRepository>();
        }

        private async void LoadDataBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Users = new(await _grainRepository!.GetUsers());
                Conversations = new(await _grainRepository.GetConversations());
                Messages = new((await _grainRepository.GetConversations()).SelectMany(c => c.Messages));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                e.Handled = true;
            }
        }

        private async void SaveDataBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _grainRepository!.Save(
                    users: Users.ToList(),
                    messages: Messages.ToList(),
                    conversations: Conversations.ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                e.Handled = true;
            }
        }

        private void ListUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListUsers.SelectedItem is UserModel selectedUser)
            {
                // state
                User = selectedUser;
                
                // users box
                UserPhone.Text = selectedUser.Phone;
                UserStatusLine.Text = selectedUser.StatusLine;
                UserDisplayName.Text = selectedUser.DisplayName;

                // conversations box
                MemberPhone.Text = selectedUser.Phone;

                // messages box
                SenderPhone.Text = selectedUser.Phone;
            }
            e.Handled = true;
        }

        private void ListConversations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListConversations.SelectedItem is ConversationModel selectedConversation)
            {
                // state
                Conversation = selectedConversation;

                // messages box
                ConversationId.Text = selectedConversation.Id;
            }
            e.Handled = true;
        }

        private void ConvoMembers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView convoMembers && convoMembers.SelectedItem is string memberId)
            {
                // conversations box
                MemberPhone.Text = memberId;

                // bubble up the selection to the parent listview
                var senderElement = (FrameworkElement)sender;
                var outerStackPanel = senderElement.GetFirstParent(el => el is StackPanel);
                if (outerStackPanel != null && outerStackPanel.DataContext is ConversationModel selectedConversation)
                {
                    ListConversations.SelectedItem = selectedConversation;
                }
            }
            e.Handled = true;
        }

        private void ListMessages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListMessages.SelectedItem is MessageModel selectedMessage)
            {
                // state
                Message = selectedMessage;

                // message box
                SenderPhone.Text = selectedMessage.Sender;
                TextMessageText.Text = selectedMessage.Text;
            }
            else
            {

            }
            e.Handled= true;
        }

        private void UserNewButton_Click(object sender, RoutedEventArgs e)
        {
            var userPhone = UserPhone.Text;
            var userStatusLine = UserStatusLine.Text;
            var userDisplayName = UserDisplayName.Text;
            var userModel = new UserModel() {
                Phone = userPhone,
                StatusLine = userStatusLine ?? "<No status line>",
                DisplayName = userDisplayName ?? "<No display name>"
            };
            Users.Add(userModel);
            e.Handled = true;
        }

        private void UserRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            Users.Remove(Users.First(u => u.Phone == UserPhone.Text));
            e.Handled = true;
        }

        private void ConvoAddMember_Click(object sender, RoutedEventArgs e)
        {
            var userPhone = MemberPhone.Text;
            if (ListConversations.SelectedItem is ConversationModel selectedConversation)
            {
                selectedConversation.Members.Add(userPhone);
                var convos = Conversations;
                Conversations = new();
                Conversations = convos;
            }
            e.Handled = true;
        }

        private void ConvoRemoveMember_Click(object sender, RoutedEventArgs e)
        {
            var userPhone = MemberPhone.Text;
            if (ListConversations.SelectedItem is ConversationModel selectedConversation)
            {
                selectedConversation.Members.Remove(userPhone);
                var convos = Conversations;
                Conversations = new();
                Conversations = convos;
            }
            e.Handled = true;
        }

        private void ConvoNewButton_Click(object sender, RoutedEventArgs e)
        {
            Conversations.Add(new ConversationModel() { Id = Guid.NewGuid().ToString() });
            e.Handled = true;
        }

        private void ConvoRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ListConversations.SelectedItem is ConversationModel selectedConversation)
            {
                Conversations.Remove(selectedConversation);
            }
            e.Handled = true;
        }

        private void MessageNewButton_Click(object sender, RoutedEventArgs e)
        {
            Messages.Add(new MessageModel()
            {
                Conversation = ConversationId.Text,
                Sender = SenderPhone.Text,
                Timestamp = DateTime.UtcNow,
                Text = TextMessageText.Text,
                Id = Guid.NewGuid()
            });
            e.Handled = true;
        }

        private void MessageRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ListMessages.SelectedItem is MessageModel selectedMessage)
            {
                Messages.Remove(selectedMessage);
            }
        }
    }
}
