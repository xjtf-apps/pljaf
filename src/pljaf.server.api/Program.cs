using Orleans;
using Orleans.Configuration;

using pljaf.server.api;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseOrleansClient((context, builder) =>
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
});

// Add services to the container.
builder.Services.AddSingleton<TwillioUserVerificationSettings>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
