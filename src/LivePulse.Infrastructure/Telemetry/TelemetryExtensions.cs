using LivePulse.Application.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LivePulse.Infrastructure.Telemetry;

public static class TelemetryExtensions
{
    // The OTLP endpoint is read from the standard OTEL_EXPORTER_OTLP_ENDPOINT environment variable.
    public static OpenTelemetryBuilder AddLivePulseTelemetry(this IServiceCollection services, string serviceName) =>
        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddSource(LivePulseTelemetry.Name)
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddRedisInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddMeter(LivePulseTelemetry.Name)
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter());
}
