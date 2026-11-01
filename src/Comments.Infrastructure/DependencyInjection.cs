using Comments.Application.Abstractions;
using Comments.Infrastructure.Captcha;
using Comments.Infrastructure.Caching;
using Comments.Infrastructure.Events;
using Comments.Infrastructure.Imaging;
using Comments.Infrastructure.Persistence;
using Comments.Infrastructure.Search;
using Comments.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Comments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(cfg.GetConnectionString("Postgres"), n => n.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var o = ConfigurationOptions.Parse(cfg["Redis:Connection"] ?? "localhost:6379");
            o.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(o);
        });

        services.Configure<StorageOptions>(cfg.GetSection("Storage"));
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IImageProcessor, ImageSharpProcessor>();
        // Captcha:Mode=LoadTest отключает проверку — ТОЛЬКО для нагрузочного теста (docker-compose.loadtest.yml).
        if (string.Equals(cfg["Captcha:Mode"], "LoadTest", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<ICaptchaService, LoadTestCaptchaService>();
        else
            services.AddSingleton<ICaptchaService, RedisCaptchaService>();
        services.AddSingleton<ICommentCache, RedisCommentCache>();
        services.AddHttpClient<ICommentSearch, ElasticCommentSearch>(c =>
        {
            c.BaseAddress = new Uri(cfg["Elastic:Url"] ?? "http://localhost:9200");
            c.Timeout = TimeSpan.FromSeconds(5);
        });
        services.AddSingleton<ICommentEventPublisher, NullCommentEventPublisher>();
        return services;
    }
}
