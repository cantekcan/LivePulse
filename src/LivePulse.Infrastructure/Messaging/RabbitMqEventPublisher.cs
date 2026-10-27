using System.Diagnostics;
using System.Text;
using LivePulse.Application.Abstractions;
using LivePulse.Application.Telemetry;
using RabbitMQ.Client;

namespace LivePulse.Infrastructure.Messaging;

public sealed class RabbitMqEventPublisher(RabbitMqConnection connection) : IEventPublisher
{
    public const string TraceParentHeader = "traceparent";

    public async Task PublishAsync(string messageType, string payload, CancellationToken cancellationToken)
    {
        using var activity = LivePulseTelemetry.ActivitySource.StartActivity($"publish {messageType}", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", connection.Options.ExchangeName);

        await using var channel = await connection.CreateChannelAsync(publisherConfirms: true, cancellationToken);

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            Type = messageType,
            Headers = new Dictionary<string, object?>()
        };

        // The span started above becomes the parent of the consumer span on the other side.
        if (activity?.Id is { } traceParent)
        {
            properties.Headers[TraceParentHeader] = traceParent;
        }

        await channel.BasicPublishAsync(
            connection.Options.ExchangeName,
            routingKey: messageType,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payload),
            cancellationToken: cancellationToken);
    }
}
