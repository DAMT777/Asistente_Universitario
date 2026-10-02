#!/usr/bin/env bash
# Genera un JWT RS256 de desarrollo para probar los servicios sin el servicio de usuarios (guía 13.2).
# Uso:  infra/scripts/generar-token-dev.sh [sub] [rol] [nombre]
# Por defecto: el profesor semilla (P0001).  Si faltan las llaves de desarrollo, las crea.
# Solo para desarrollo local: las llaves nunca se suben al repositorio (infra/keys/ está en .gitignore).
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LLAVES="$RAIZ/infra/keys"
SUB="${1:-a0000000-0000-0000-0000-000000000001}"
ROL="${2:-PROFESOR}"
NOMBRE="${3:-Profesor Demo}"
ISS="${JWT_ISSUER:-unillanos-usuarios}"
AUD="${JWT_AUDIENCE:-unillanos-notas}"
MINUTOS="${JWT_MINUTOS:-60}"

if [[ ! -f "$LLAVES/jwt-private.pem" ]]; then
  mkdir -p "$LLAVES"
  openssl genrsa -out "$LLAVES/jwt-private.pem" 2048 2>/dev/null
  openssl rsa -in "$LLAVES/jwt-private.pem" -pubout -out "$LLAVES/jwt-public.pem" 2>/dev/null
  echo "Llaves de desarrollo creadas en $LLAVES" >&2
fi

b64url() { openssl base64 -A | tr '+/' '-_' | tr -d '='; }

AHORA=$(date +%s)
CABECERA='{"alg":"RS256","typ":"JWT"}'
CARGA=$(printf '{"sub":"%s","rol":"%s","nombre":"%s","iss":"%s","aud":"%s","iat":%s,"exp":%s}' \
  "$SUB" "$ROL" "$NOMBRE" "$ISS" "$AUD" "$AHORA" "$((AHORA + MINUTOS * 60))")

FIRMANTE="$(printf '%s' "$CABECERA" | b64url).$(printf '%s' "$CARGA" | b64url)"
FIRMA=$(printf '%s' "$FIRMANTE" | openssl dgst -sha256 -sign "$LLAVES/jwt-private.pem" | b64url)
printf '%s.%s\n' "$FIRMANTE" "$FIRMA"
