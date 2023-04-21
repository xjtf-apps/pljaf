namespace voks.server.records
{
    public class ConversationModel
    {
        public string Id { get; set; }
        public string Kind { get; set; } = "<Unknown>";
        public string Name { get; set; } = "";
        public string Topic { get; set; } = "";
        public List<string> Members { get; set; } = new();
        public List<MessageModel> Messages { get; set; } = new();

        public override bool Equals(object? obj)
        {
            if (obj is ConversationModel other)
            {
                if (Id == other.Id) return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
