#!/bin/bash
# Restores ArdhDb from a .bak file produced by backup-db.sh.
# Usage: ./deploy/restore-db.sh backups/ArdhDb-20260815-190642.bak
set -euo pipefail
cd "$(dirname "$0")/.."

if [ -z "${1:-}" ]; then
  echo "Usage: $0 <path-to-backup.bak>"
  exit 1
fi
if [ ! -f "$1" ]; then
  echo "File not found: $1"
  exit 1
fi

read -p "This will REPLACE all data in the live database with the contents of $1. Type 'yes' to continue: " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
  echo "Aborted."
  exit 1
fi

docker compose exec db mkdir -p /var/opt/mssql/backup
docker cp "$1" ardh-db:/var/opt/mssql/backup/restore.bak

docker compose exec db bash -c 'sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "ALTER DATABASE ArdhDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE ArdhDb FROM DISK = N'\''/var/opt/mssql/backup/restore.bak'\'' WITH REPLACE; ALTER DATABASE ArdhDb SET MULTI_USER;"'

echo "Restore complete. Consider restarting the api container: docker compose restart api"
