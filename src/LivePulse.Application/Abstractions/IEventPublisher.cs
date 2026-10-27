namespace LivePulse.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(string messageType, string payload, CancellationToken cancellationToken);
}
