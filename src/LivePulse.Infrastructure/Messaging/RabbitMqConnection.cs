using LivePulse.Application.Coupons.PlaceCoupon;
using LivePulse.Application.Matches;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LivePulse.Infrastructure.Messaging;

public sealed class RabbitMqConnection(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqOptions Options => options.Value;

    public async Task<IChannel> CreateChannelAsync(bool publisherConfirms, CancellationToken cancellationToken)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: publisherConfirms,
            publisherConfirmationTrackingEnabled: publisherConfirms);

        var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
        await DeclareTopologyAsync(channel, cancellationToken);
        return channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        await channel.ExchangeDeclareAsync(
            settings.ExchangeName, ExchangeType.Direct, durable: true, cancellationToken: cancellationToken);

        await BindQueueAsync(channel, settings.MatchEventsQueue, nameof(MatchEventMessage), cancellationToken);
        await BindQueueAsync(channel, settings.CouponEventsQueue, nameof(CouponPlacedMessage), cancellationToken);
    }

    private async Task BindQueueAsync(IChannel channel, string queue, string routingKey, CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queue, options.Value.ExchangeName, routingKey, cancellationToken: cancellationToken);
    }
}
