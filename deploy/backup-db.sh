#!/bin/bash
# Creates a timestamped SQL Server backup and copies it out of the container
# into ./backups/ on the host. Run from anywhere; it cd's to the repo root itself.
set -euo pipefail
cd "$(dirname "$0")/.."

mkdir -p backups
TS=$(date +%Y%m%d-%H%M%S)

docker compose exec db bash -c 'mkdir -p /var/opt/mssql/backup && /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "BACKUP DATABASE ArdhDb TO DISK = N'\''/var/opt/mssql/backup/ArdhDb.bak'\'' WITH FORMAT, INIT"'

docker cp ardh-db:/var/opt/mssql/backup/ArdhDb.bak "backups/ArdhDb-${TS}.bak"
echo "Backup saved to backups/ArdhDb-${TS}.bak"
