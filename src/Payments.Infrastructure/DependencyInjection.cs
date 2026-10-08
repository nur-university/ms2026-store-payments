using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Payments.Infrastructure.Extensions;
using System.Reflection;

namespace Payments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName)
    {
        services.AddMediatR(config =>
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
            );

        services.AddSecrets(configuration, environment)
            .AddPersistence()
            .AddSecurity(environment)
            .AddRabbitMQ(environment)
            .AddObservability(environment, serviceName);

        return services;
    }
}
