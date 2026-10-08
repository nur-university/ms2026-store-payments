using Joseco.Communication.External.Contracts.Services;
using Joseco.Communication.External.RabbitMQ.Services;
using Joseco.Communication.External.RabbitMQ;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nur.Store2026.Integration.Orders;
using Payments.Application.Payments.CreatePayment;
using Payments.Infrastructure.Observability;

namespace Payments.Infrastructure.Extensions;

public static class BrokerExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IHostEnvironment environment)
    {
        using var serviceProvider = services.BuildServiceProvider();
        var rabbitMqSettings = serviceProvider.GetRequiredService<RabbitMqSettings>();

        bool isWebApp = environment is IWebHostEnvironment;
        services.AddRabbitMQ(rabbitMqSettings);

        if (isWebApp)
        {
            return services;
        }

        services.AddRabbitMqConsumer<OrderReserved, CreatePaymentHandler>("payments-order-reserved");

        services.Decorate<IIntegrationMessageConsumer<OrderReserved>, TracingConsumer<OrderReserved>>();

        return services;
    }
}
