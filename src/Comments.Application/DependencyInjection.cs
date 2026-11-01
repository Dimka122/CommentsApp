using Comments.Application.Abstractions;
using Comments.Application.Comments;
using Comments.Application.Html;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IHtmlSanitizer, HtmlSanitizer>();
        services.AddScoped<CommentService>();
        return services;
    }
}
