using CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Application.Interfaces;
using CommentsApp.CommentsApp.Application.Services;
using CommentsApp.CommentsApp.Application.Validation;
using CommentsApp.CommentsApp.Infrastructure.Captcha;
using CommentsApp.CommentsApp.Infrastructure.Data;
using CommentsApp.CommentsApp.Infrastructure.Security;
using CommentsApp.CommentsApp.Infrastructure.Services;
using CommentsApp.Infrastructure.Repositories;
using CommentsApp.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// EF Core + MSSQL
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Слои
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddScoped<IFileStorage, FileStorage>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddValidatorsFromAssemblyContaining<CommentCreateValidator>();

// Контроллеры
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Автоматическая миграция БД при старте (удобно для Docker)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();   // wwwroot/index.html
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
