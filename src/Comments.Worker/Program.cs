using Comments.Infrastructure;
using Comments.Infrastructure.Messaging;
using Comments.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);   // БД и Elasticsearch (Redis/файлы создаются лениво и здесь не нужны)
builder.Services.AddMessaging(builder.Configuration, x => x.AddConsumer<CommentIndexConsumer>());
await builder.Build().RunAsync();
