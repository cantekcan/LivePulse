using LivePulse.Infrastructure;
using LivePulse.Infrastructure.Telemetry;
using LivePulse.Simulator;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddLivePulseTelemetry("livepulse-simulator");
builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection(SimulatorOptions.SectionName));
builder.Services.AddSingleton<MatchEventGenerator>();
builder.Services.AddHttpClient<MatchApiClient>((provider, client) =>
    client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<SimulatorOptions>>().Value.ApiBaseUrl));
builder.Services.AddHostedService<Worker>();

builder.Build().Run();
