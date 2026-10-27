using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches.Rotation;
using Microsoft.Extensions.DependencyInjection;

namespace LivePulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<IClock>()
            .AddClasses(classes => classes.AssignableToAny(typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<MatchRotator>();

        return services;
    }
}
