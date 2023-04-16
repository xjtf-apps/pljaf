using Orleans;

namespace voks.server.model;

[GenerateSerializer]
public class Options
{
    [Id(0)] public bool SendNotifications { get; set; } = true;
}
