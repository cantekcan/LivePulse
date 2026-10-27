namespace LivePulse.Api.BackgroundServices;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollIntervalSeconds { get; set; } = 3;

    public int BatchSize { get; set; } = 50;
}
