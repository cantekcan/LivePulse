namespace LivePulse.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
