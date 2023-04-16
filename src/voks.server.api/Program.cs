using Orleans;
using System.Text;
using Orleans.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.AspNetCore.Authentication.JwtBearer;

using voks.server.api;

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
builder.Services.AddTransient<TwillioSettingsService>();
builder.Services.AddTransient<MediaSettingsService>();
builder.Services.AddTransient<JwtSettingsService>();
builder.Services.AddTransient<JwtTokenService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,

        ValidAudience = builder.Configuration["Jwt:ValidAudience"]!,
        ValidIssuer = builder.Configuration["Jwt:ValidIssuer"]!,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo()
    {
        Title = "voks API",
        Version = "v1"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        In = ParameterLocation.Header,
        Description = "Please enter Jwt bearer token",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        { new OpenApiSecurityScheme() { Reference = new OpenApiReference() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }}, new string[] { } }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebSockets();
app.MapControllers();
app.Run();
