#!/usr/bin/env bash
# Создаёт solution и первую миграцию. Нужны .NET 9 SDK и: dotnet tool install -g dotnet-ef
set -e
dotnet new sln -n CommentsApp --force
dotnet sln add src/*/*.csproj tests/*/*.csproj
# Миграцию создаём один раз; после изменения модели используйте другое имя.
[ -d src/Comments.Infrastructure/Persistence/Migrations ] || \
  dotnet ef migrations add Initial -p src/Comments.Infrastructure -s src/Comments.Api -o Persistence/Migrations
dotnet test
