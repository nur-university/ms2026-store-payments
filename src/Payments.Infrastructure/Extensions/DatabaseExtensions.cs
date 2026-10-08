using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.Contracts.Service;
using Joseco.Outbox.EFCore;
using Joseco.Outbox.EFCore.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payments.Domain.Payments;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence.DomainModel;
using Payments.Infrastructure.Persistence.Repositories;
using Payments.Infrastructure.Persistence.StoredModel;

namespace Payments.Infrastructure.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        var databaseSettings = services.BuildServiceProvider().GetRequiredService<DataBaseSettings>();
        var dbConnectionString = databaseSettings.ConnectionString;

        services.AddDbContext<StoredDbContext>(context =>
                context.UseNpgsql(dbConnectionString));
        services.AddDbContext<DomainDbContext>(context =>
                context.UseNpgsql(dbConnectionString));

        services.AddScoped<IDatabase, StoredDbContext>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IOutboxDatabase<DomainEvent>, UnitOfWork>();
        services.AddOutbox<DomainEvent>();

        services.Decorate<IOutboxService<DomainEvent>, OutboxTracingService<DomainEvent>>();

        return services;
    }
}
