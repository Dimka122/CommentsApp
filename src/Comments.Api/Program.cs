using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Comments.Api;
using Comments.Application;
using Comments.Api.GraphQL;
using Comments.Api.Hubs;
using Comments.Infrastructure;
using Comments.Infrastructure.Messaging;
using Comments.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
// RabbitMQ (MassTransit): публикуем CommentCreated и слушаем его для рассылки по SignalR.
// Temporary = у каждого экземпляра API своя временная очередь, поэтому событие получают ВСЕ экземпляры
// (иначе при нескольких API клиенты другого экземпляра не увидели бы новый комментарий).
builder.Services.AddMessaging(builder.Configuration, x =>
    x.AddConsumer<CommentBroadcastConsumer>().Endpoint(e => e.Temporary = true));

builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<CommentGqlType>()
    .AddType<AttachmentGqlType>()
    .AddMaxExecutionDepthRule(8);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddSignalR()
    .AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Мы за reverse proxy (Caddy/nginx) в docker-сети, поэтому доверяем X-Forwarded-*.
// Не публикуйте порт API наружу в проде — только через прокси.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var writeLimit = builder.Configuration.GetValue("RateLimiting:WritePerMinute", 20);
var readLimit = builder.Configuration.GetValue("RateLimiting:ReadPerMinute", 120);
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    RateLimitPartition<string> ByIp(HttpContext c, int limit) =>
        RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1) });
    o.AddPolicy("write", c => ByIp(c, writeLimit));
    o.AddPolicy("read", c => ByIp(c, readLimit));
});

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
if (origins.Length > 0)
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["Referrer-Policy"] = "same-origin";
    await next();
});
if (origins.Length > 0) app.UseCors();
app.UseRateLimiter();

app.MapControllers();
app.MapGraphQL("/graphql").RequireRateLimiting("read");
app.MapHub<CommentsHub>("/hubs/comments");
app.MapHealthChecks("/health");
app.Run();
