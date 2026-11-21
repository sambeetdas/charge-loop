using System.Diagnostics;
using System.Security.Claims;
using ChargeLoop.Domain.Entities;
using ChargeLoop.Infrastructure.Persistence;

namespace ChargeLoop.WebApi.Middleware;

public class CorrelationAndAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationAndAuditMiddleware> _logger;
    private const string HeaderName = "X-Correlation-Id";

    public CorrelationAndAuditMiddleware(RequestDelegate next, ILogger<CorrelationAndAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms (CorrelationId={CorrelationId}, ActorRole={ActorRole})",
                    context.Request.Method,
                    context.Request.Path.Value,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds,
                    correlationId,
                    actorRole ?? "Anonymous");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Audit logging failed for {Path} (CorrelationId={CorrelationId})", context.Request.Path.Value, correlationId);
            }
        }
    }
}
