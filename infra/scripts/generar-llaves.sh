#!/usr/bin/env bash
# Genera el par de llaves RSA de DESARROLLO para firmar/verificar JWT RS256.
# Quedan en infra/keys/ (ignorada por git). Nunca usar estas llaves fuera de desarrollo.
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/keys"
mkdir -p "$DIR"

if [[ -f "$DIR/jwt-privada.pem" && "${1:-}" != "--forzar" ]]; then
  echo "Ya existen llaves en $DIR (use --forzar para regenerarlas)."
  exit 0
fi

openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out "$DIR/jwt-privada.pem" 2>/dev/null
openssl pkey -in "$DIR/jwt-privada.pem" -pubout -out "$DIR/jwt-publica.pem"
chmod 600 "$DIR/jwt-privada.pem"
chmod 644 "$DIR/jwt-publica.pem"
echo "Llaves de desarrollo creadas en $DIR"
