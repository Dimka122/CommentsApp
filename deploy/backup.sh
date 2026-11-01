#!/usr/bin/env bash
# Ежедневный дамп БД (cron: 0 3 * * * /opt/comments/deploy/backup.sh). Хранит 14 дней.
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p backups
docker compose exec -T postgres pg_dump -U app comments | gzip > "backups/comments-$(date +%F-%H%M).sql.gz"
find backups -name 'comments-*.sql.gz' -mtime +14 -delete
