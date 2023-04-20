namespace voks.server.records
{
    public class UserModel
    {
        public string Phone { get; set; }
        public string DisplayName { get; set; }
        public string StatusLine { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is UserModel other)
            {
                if (Phone == other.Phone) return true;
            }
            return false;
        }

        public override int GetHashCode() => Phone.GetHashCode();
    }
}
