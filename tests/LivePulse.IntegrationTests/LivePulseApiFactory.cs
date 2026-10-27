using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace LivePulse.IntegrationTests;

public sealed class LivePulseApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const int RabbitMqPort = 5672;
    private const string OutboxPollsNever = "3600";

    private readonly MsSqlContainer _sqlServer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine").Build();
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_sqlServer.StartAsync(), _redis.StartAsync(), _rabbitMq.StartAsync());
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await Task.WhenAll(_sqlServer.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask(), _rabbitMq.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:SqlServer", _sqlServer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.UseSetting("RabbitMq:Host", _rabbitMq.Hostname);
        builder.UseSetting("RabbitMq:Port", _rabbitMq.GetMappedPublicPort(RabbitMqPort).ToString());
        builder.UseSetting("RabbitMq:Username", "guest");
        builder.UseSetting("RabbitMq:Password", "guest");

        // Keeps outbox rows unprocessed so the tests can see them.
        builder.UseSetting("Outbox:PollIntervalSeconds", OutboxPollsNever);
    }
}
