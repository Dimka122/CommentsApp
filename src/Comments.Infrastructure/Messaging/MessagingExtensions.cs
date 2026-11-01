using Comments.Application.Abstractions;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Comments.Infrastructure.Messaging;

public static class MessagingExtensions
{
    /// <summary>RabbitMQ через MassTransit. API вызывает без параметров (только публикует), Worker добавляет консьюмеры.</summary>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services, IConfiguration cfg, Action<IBusRegistrationConfigurator>? configure = null)
    {
        services.AddMassTransit(x =>
        {
            configure?.Invoke(x);
            x.UsingRabbitMq((ctx, bus) =>
            {
                bus.Host(cfg["RabbitMq:Host"] ?? "localhost", "/", h =>
                {
                    h.Username(cfg["RabbitMq:Username"] ?? "guest");
                    h.Password(cfg["RabbitMq:Password"] ?? "guest");
                });
                bus.UseMessageRetry(r => r.Intervals(500, 2000, 5000));
                bus.ConfigureEndpoints(ctx);
            });
        });

        services.Replace(ServiceDescriptor.Scoped<ICommentEventPublisher, MassTransitCommentEventPublisher>());
        return services;
    }
}
