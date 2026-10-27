namespace LivePulse.Application.Abstractions;

public interface IOutboxWriter
{
    void Add<TMessage>(TMessage message);
}
