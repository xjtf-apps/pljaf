namespace voks.server.records
{
    public class MessageModel
    {
        public string Conversation { get; set; }
        public Guid Id { get; set; }
        public string Sender { get; set; }
        public DateTime Timestamp { get; set; }
        public string Text { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is MessageModel other)
            {
                if (Conversation == other.Conversation &&
                    Sender == other.Sender &&
                    Timestamp == other.Timestamp)
                    return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return
                Conversation.GetHashCode() ^
                Sender.GetHashCode() ^
                Timestamp.GetHashCode();
        }
    }
}
