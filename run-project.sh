#!/usr/bin/env bash
# Starts the InspectFlow stack (db + api + web) with Docker Compose.
#
# Usage:
#   ./run-project.sh          build and start everything, wait until it is ready
#   ./run-project.sh stop     stop the containers (data is kept)
#   ./run-project.sh reset    stop and delete all data (database + uploaded files)
#   ./run-project.sh logs     follow the logs of all services
set -euo pipefail

cd "$(dirname "$0")"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker not found. Install Docker Desktop and try again." >&2
  exit 1
fi
if ! docker info >/dev/null 2>&1; then
  echo "Docker is not running. Start Docker Desktop and try again." >&2
  exit 1
fi

case "${1:-up}" in
  stop)  docker compose down; exit 0 ;;
  reset) docker compose down -v; exit 0 ;;
  logs)  docker compose logs -f; exit 0 ;;
  up)    ;;
  *)     echo "Unknown command: $1 (use: up | stop | reset | logs)" >&2; exit 1 ;;
esac

if [ ! -f .env ]; then
  echo "No .env found; creating one from .env.example."
  cp .env.example .env
fi

# Read a value from .env, falling back to a default.
env_value() {
  local v
  v=$(grep -E "^$1=" .env | tail -1 | cut -d= -f2- | sed 's/[[:space:]]*#.*$//; s/[[:space:]]*$//')
  echo "${v:-$2}"
}
WEB_PORT=$(env_value WEB_PORT 3000)
API_PORT=$(env_value API_PORT 8080)

echo "Building and starting containers..."
docker compose up --build -d

echo -n "Waiting for the web app"
for _ in $(seq 1 90); do
  if curl -fs -o /dev/null "http://localhost:${WEB_PORT}"; then
    echo " ready."
    break
  fi
  echo -n "."
  sleep 2
done

if ! curl -fs -o /dev/null "http://localhost:${WEB_PORT}"; then
  echo
  echo "The web app did not respond in time. Check the logs with: ./run-project.sh logs" >&2
  docker compose ps
  exit 1
fi

cat <<EOF

InspectFlow is running:
  Web app : http://localhost:${WEB_PORT}
  API     : http://localhost:${API_PORT}  (health: /health)

Demo users (password Demo@12345):
  company@demo.local | agent@demo.local | tenant@demo.local

Stop with: ./run-project.sh stop
EOF
