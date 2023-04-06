using Orleans.Configuration;
using pljaf.server.actors.app;
using pljaf.server.model;

await Host
    .CreateDefaultBuilder(args)
    .UseOrleans((context, builder) =>
    {
        var hostConfig = context.Configuration;
        var actorSystemSection = hostConfig.GetRequiredSection("ActorSystem");
        var actorSystemSettings = actorSystemSection.Get<ActorSystemSettings>()!;

        var siloPorts = actorSystemSettings.Ports.SiloPorts;
        var gatewayPorts = actorSystemSettings.Ports.GatewayPorts;
        var connectionStrings = actorSystemSettings.ConnectionStrings;

        builder.ConfigureEndpoints(siloPort: siloPorts, gatewayPort: gatewayPorts);
        builder.Configure<ClusterOptions>(co => co.ClusterId = actorSystemSettings.ClusterId);
        builder.Configure<ClusterOptions>(co => co.ServiceId = actorSystemSettings.ServiceId);
        builder.ConfigureLogging(builder => builder.SetMinimumLevel(LogLevel.Information).AddConsole());
        builder.UseAzureStorageClustering(options => options.ConfigureTableServiceClient(connectionStrings.MembershipTable));
        builder.AddAzureTableGrainStorageAsDefault(options => options.ConfigureTableServiceClient(connectionStrings.ActorPersistance));
        builder.AddAzureBlobGrainStorage(Constants.Stores.MediaStore, options => options.ConfigureBlobServiceClient(connectionStrings.BinaryDataStorage));
    })
    .RunConsoleAsync();
