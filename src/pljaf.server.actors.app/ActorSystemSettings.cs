namespace pljaf.server.actors.app;
public class ActorSystemSettings
{
    public required PortSettings Ports { get; set; }
    public required ConnectionStringSettings ConnectionStrings { get; set; }
    public required string ClusterId { get; set; }
    public required string ServiceId { get; set; }

    public class PortSettings
    {
        public required int SiloPorts { get; set; }
        public required int GatewayPorts { get; set; }
    }

    public class ConnectionStringSettings
    {
        public required string MembershipTable { get; set; }
        public required string ActorPersistance { get; set; }
        public required string BinaryDataStorage { get; set; }
    }
}