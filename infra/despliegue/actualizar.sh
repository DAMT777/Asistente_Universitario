#!/usr/bin/env bash
# Baja los cambios del repositorio y reconstruye lo necesario. Desde la raíz del repositorio.
set -euo pipefail
git pull --ff-only
docker compose -f infra/despliegue/docker-compose.aws.yml up --build -d
docker compose -f infra/despliegue/docker-compose.aws.yml ps
