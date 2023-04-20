namespace voks.server.api;

// WARN: copied from voks.server.api project!

public class ActorSystemSettings
{
    public required string ClusterId { get; set; }
    public required string ServiceId { get; set; }
    public required string ClusteringConnectionString { get; set; }
}