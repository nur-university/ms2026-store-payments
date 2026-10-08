using Nur.Store2026.Observability.Tracing;
using Payments.Application.Abstractions;
using Serilog.Context;

namespace Payments.WebApi.Middleware;

public class RequestContextLoggingMiddleware(RequestDelegate next)
{
    public Task Invoke(HttpContext context, ITracingProvider tracingProvider)
    { 
        using (LogContext.PushProperty("CorrelationId", tracingProvider.GetCorrelationId()))
        {
            return next.Invoke(context);
        }
    }
}
