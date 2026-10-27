using System.Diagnostics;
using System.Text;
using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Application.Matches.ProcessMatchEvent;
using LivePulse.Application.Telemetry;
using LivePulse.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LivePulse.Api.BackgroundServices;

public sealed class MatchEventConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<MatchEventConsumer> logger) : BackgroundService
{
    private const ushort PrefetchCount = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var channel = await StartConsumingAsync(stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "RabbitMQ consumer failed, retrying in {Delay}", RetryDelay);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }

    private async Task<IChannel> StartConsumingAsync(CancellationToken cancellationToken)
    {
        var channel = await connection.CreateChannelAsync(publisherConfirms: false, cancellationToken);
        await channel.BasicQosAsync(0, PrefetchCount, false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleDeliveryAsync(channel, delivery, cancellationToken);

        await channel.BasicConsumeAsync(connection.Options.MatchEventsQueue, autoAck: false, consumer, cancellationToken);
        return channel;
    }

    private async Task HandleDeliveryAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        using var activity = StartConsumerActivity(delivery);
        try
        {
            await ProcessAsync(delivery, cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Could not process match event, dropping the message");
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
    }

    private static Activity? StartConsumerActivity(BasicDeliverEventArgs delivery)
    {
        ActivityContext parent = default;
        if (delivery.BasicProperties.Headers?.TryGetValue(RabbitMqEventPublisher.TraceParentHeader, out var header) == true
            && header is byte[] bytes)
        {
            ActivityContext.TryParse(Encoding.UTF8.GetString(bytes), null, out parent);
        }

        var activity = LivePulseTelemetry.ActivitySource.StartActivity("process match-event", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        return activity;
    }

    private async Task ProcessAsync(BasicDeliverEventArgs delivery, CancellationToken cancellationToken)
    {
        var json = Encoding.UTF8.GetString(delivery.Body.Span);
        var message = MessageSerializer.Deserialize<MatchEventMessage>(json)
            ?? throw new InvalidOperationException("The message body is empty.");

        var command = new ProcessMatchEventCommand(
            message.MatchId, message.Type, message.Side, message.Minute, message.OccurredAt);

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ProcessMatchEventCommand, MatchDto>>();
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Match event rejected: {Code}", result.Error!.Code);
        }
    }
}
