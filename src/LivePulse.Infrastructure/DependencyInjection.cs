using LivePulse.Application.Abstractions;
using LivePulse.Infrastructure.Messaging;
using LivePulse.Infrastructure.Outbox;
using LivePulse.Infrastructure.Persistence;
using LivePulse.Infrastructure.Redis;
using LivePulse.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace LivePulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LivePulseDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServer")));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<LivePulseDbContext>());
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectRedis(configuration));
        services.AddSingleton<IOddsStore, RedisOddsStore>();

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        return services;
    }

    private static ConnectionMultiplexer ConnectRedis(IConfiguration configuration)
    {
        var options = ConfigurationOptions.Parse(configuration.GetConnectionString("Redis") ?? "localhost:6379");
        options.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(options);
    }
}
