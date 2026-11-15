using System.Diagnostics;
using System.Security.Claims;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;

namespace ChargeLoop.WebApi.Middleware;

public class CorrelationAndAuditMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-Id";

    public CorrelationAndAuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ChargeLoopDbContext dbContext)
    {
        Guid correlationId = Guid.NewGuid();
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerVal) &&
            Guid.TryParse(headerVal.FirstOrDefault(), out var parsed))
        {
            correlationId = parsed;
        }

        context.Items["CorrelationId"] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId.ToString();
            return Task.CompletedTask;
        });

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            try
            {
                var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
                Guid.TryParse(sub, out var actorId);
                var actorRole = context.User.FindFirstValue(ClaimTypes.Role);

                await dbContext.AuditLogs.AddAsync(new AuditLog
                {
                    CorrelationId = correlationId,
                    EventName = "HTTP_REQUEST",
                    ActorId = actorId == Guid.Empty ? null : actorId,
                    ActorRole = actorRole,
                    HttpMethod = context.Request.Method,
                    Endpoint = context.Request.Path.Value,
                    StatusCode = context.Response.StatusCode,
                    DurationMs = sw.ElapsedMilliseconds,
                    CreatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Avoid breaking request flow if audit logging fails
            }
        }
    }
}
