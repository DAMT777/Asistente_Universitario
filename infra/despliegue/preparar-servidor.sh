#!/usr/bin/env bash
# Prepara una instancia Ubuntu 24.04 recién creada (EC2): Docker, swap de 2 GB, llaves JWT y archivo .env.
# Uso (como usuario ubuntu, desde la raíz del repositorio ya clonado): bash infra/despliegue/preparar-servidor.sh
set -euo pipefail

sudo apt-get update -y
sudo apt-get install -y docker.io docker-compose-v2 git openssl
sudo usermod -aG docker "$USER"

if ! swapon --show | grep -q /swapfile; then
  sudo fallocate -l 2G /swapfile
  sudo chmod 600 /swapfile
  sudo mkswap /swapfile
  sudo swapon /swapfile
  grep -q '/swapfile' /etc/fstab || echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab >/dev/null
fi

if [ ! -f infra/keys/jwt-private.pem ]; then
  bash infra/scripts/generar-llaves.sh
fi

if [ ! -f infra/despliegue/.env ]; then
  cp infra/despliegue/aws.env.example infra/despliegue/.env
  sed -i "s/^HOST_UID=.*/HOST_UID=$(id -u)/; s/^HOST_GID=.*/HOST_GID=$(id -g)/; s/^SERVICE_KEY=.*/SERVICE_KEY=$(openssl rand -hex 24)/" infra/despliegue/.env
  echo ">> Creado infra/despliegue/.env. Edítelo (PUBLIC_URL, SQL_SA_PASSWORD, LLM_API_KEY) antes de levantar."
fi

echo ">> Listo. Cierre la sesión SSH y vuelva a entrar para que aplique el grupo docker."
