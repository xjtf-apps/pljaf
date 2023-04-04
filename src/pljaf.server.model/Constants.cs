﻿namespace pljaf.server.model;

public static class Constants
{
    public static class StoreKeys
    {
        public static class User
        {
            public const string Options = "v1/user/options";
            public const string Profile = "v1/user/profile";
            public const string Contacts = "v1/user/contacts";
            public const string Conversations = "v1/user/conversations";
        }

        public static class Conversation
        {
            public const string Name = "v1/conversation/name";
            public const string Topic = "v1/conversation/topic";
            public const string Members = "v1/conversation/members";
            public const string Invitations = "v1/conversation/invitations";
            public const string Communications = "v1/conversations/communications";
        }

        public static class Message
        {
            public const string Sender = "v1/message/sender";
            public const string Timestamp = "v1/message/timestamp";
            public const string EncryptedTextData = "v1/message/encrypted_text_data";
            public const string MediaReference = "v1/message/media_reference";
        }
    }
}
