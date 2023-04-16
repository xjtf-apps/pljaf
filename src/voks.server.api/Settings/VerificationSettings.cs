namespace voks.server.api;

public class VerificationSettings
{
    public required TwillioSettings Twillio { get; set; }

    public class TwillioSettings
    {
        public required string AuthToken { get; set; }
        public required string AccountSid { get; set; }
        public required string ServiceSid { get; set; }
    }
}