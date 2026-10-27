using LivePulse.Application.Abstractions;
using LivePulse.Infrastructure.Messaging;
using LivePulse.Infrastructure.Persistence;

namespace LivePulse.Infrastructure.Outbox;

public sealed class OutboxWriter(LivePulseDbContext dbContext, IClock clock) : IOutboxWriter
{
    public void Add<TMessage>(TMessage message)
    {
        var payload = MessageSerializer.Serialize(message);
        dbContext.OutboxMessages.Add(new OutboxMessage(typeof(TMessage).Name, payload, clock.UtcNow));
    }
}
