using CommentsApp.Application.Interfaces;
using CommentsApp.Application.Services;
using CommentsApp.Application.Validation;
using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Infrastructure.Captcha;
using CommentsApp.Infrastructure.Data;
using CommentsApp.Infrastructure.Repositories;
using CommentsApp.Infrastructure.Security;
using CommentsApp.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IFileStorage, FileStorage>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddValidatorsFromAssemblyContaining<CommentCreateValidator>();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("post-comment", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
