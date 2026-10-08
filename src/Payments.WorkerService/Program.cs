using Payments.Application;
using Payments.Infrastructure;
using Joseco.DDD.Core.Abstractions;
using Joseco.Outbox.EFCore;
using Nur.Store2026.Observability;

var builder = Host.CreateApplicationBuilder(args);

string serviceName = "payments.worker-service";

builder.UseLogging(serviceName, builder.Configuration);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment, serviceName);
builder.Services.AddOutboxBackgroundService<DomainEvent>();

var host = builder.Build();
host.Run();
