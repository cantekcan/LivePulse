using System.Text.Json.Serialization;
using LivePulse.Api.BackgroundServices;
using LivePulse.Api.Hubs;
using LivePulse.Api.Startup;
using LivePulse.Application;
using LivePulse.Application.Abstractions;
using LivePulse.Infrastructure;
using LivePulse.Infrastructure.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IOddsNotifier, SignalROddsNotifier>();

builder.Services
    .AddLivePulseTelemetry("livepulse-api")
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(options =>
        options.Filter = context => context.Request.Path != "/health"))
    .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection(OutboxOptions.SectionName));
builder.Services.AddHostedService<MatchEventConsumer>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services, app.Lifetime.ApplicationStopping);

app.UseSwagger();
app.UseSwaggerUI();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
app.MapControllers();
app.MapHub<OddsHub>(OddsHub.Path);

app.Run();

public partial class Program;
