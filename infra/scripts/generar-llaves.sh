#!/usr/bin/env bash
# Genera el par de llaves RSA de desarrollo para firmar y verificar el JWT (guía técnica, sección 10.4).
# Cada integrante genera el suyo. infra/keys/ está en el .gitignore y nunca se sube.
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p keys
openssl genrsa -out keys/jwt-private.pem 2048
openssl rsa -in keys/jwt-private.pem -pubout -out keys/jwt-public.pem
echo "Llaves generadas en infra/keys/"
