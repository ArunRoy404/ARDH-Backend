#!/bin/bash
# Wipes the live database via the app's built-in SEED_MODE, then clears the
# flag again so a future container restart/reboot doesn't wipe it a second time.
# Usage: ./deploy/reset-db.sh reset   (wipe + re-seed full demo dataset)
#        ./deploy/reset-db.sh wipe    (wipe, keep only the admin user)
set -euo pipefail
cd "$(dirname "$0")/.."

MODE="${1:-}"
if [ "$MODE" != "reset" ] && [ "$MODE" != "wipe" ]; then
  echo "Usage: $0 <reset|wipe>"
  echo "  reset - wipe ALL data and re-seed the full demo dataset"
  echo "  wipe  - wipe ALL data, keep only the admin user"
  exit 1
fi

read -p "This will WIPE ALL DATA in the live database (mode: $MODE). Type 'yes' to continue: " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
  echo "Aborted."
  exit 1
fi

echo "==> Restarting api with SEED_MODE=$MODE ..."
SEED_MODE="$MODE" docker compose up -d --force-recreate api

echo "==> Waiting 20s for startup + seeding to finish..."
sleep 20
docker compose logs --tail=50 api

echo "==> Recreating api WITHOUT SEED_MODE so future restarts don't wipe again..."
docker compose up -d --force-recreate api

echo "==> Done."
