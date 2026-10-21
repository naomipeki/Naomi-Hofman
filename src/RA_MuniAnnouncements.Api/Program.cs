using RA_MuniAnnouncements.Api.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (await app.TryRunCommandAsync(args))
{
    return;
}

app.UseApplicationMiddleware();
app.MapApplicationEndpoints();

app.Run();

/// <summary>Exposed for future WebApplicationFactory integration tests.</summary>
public partial class Program;
