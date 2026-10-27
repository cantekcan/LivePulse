namespace LivePulse.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string ExchangeName { get; set; } = "match-events";

    public string MatchEventsQueue { get; set; } = "match-events";

    public string CouponEventsQueue { get; set; } = "coupon-events";
}
