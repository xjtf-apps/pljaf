using Orleans.Hosting;
using Orleans.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// WARN: copied source code, may diverge
using voks.server.api;

namespace voks.server.records;

public partial class Program
{
    public static void Main(string[] args)
    {
        Host.CreateDefaultBuilder(args)
            .UseOrleansClient(ConfigureOrleansClient)
            .ConfigureServices(ConfigureServices)
            .Build()
            .Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHostedService<WpfWorker>();
        services.AddTransient<GrainRepository>();
        services.AddSingleton<ApplicationTermination>();
    }

    private static void ConfigureOrleansClient(HostBuilderContext context, IClientBuilder builder)
    {
        var hostConfig = context.Configuration;
        var actorSystemSection = hostConfig.GetRequiredSection("ActorSystem");
        var actorSystemConfig = actorSystemSection.Get<ActorSystemSettings>()!;

        builder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = actorSystemConfig.ClusterId;
            options.ServiceId = actorSystemConfig.ServiceId;
        });
        builder.UseAzureStorageClustering(options => options.ConfigureTableServiceClient(actorSystemConfig.ClusteringConnectionString));
    }
}