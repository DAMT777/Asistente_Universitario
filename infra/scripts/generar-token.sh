#!/usr/bin/env bash
# Emite un JWT RS256 de PRUEBA para un usuario semilla, firmado con infra/keys/jwt-private.pem.
# Sustituye al servicio de Usuarios mientras no esté disponible. Solo para desarrollo.
#
#   TOKEN=$(infra/scripts/generar-token.sh ana)
#   infra/scripts/generar-token.sh profesor --minutos 5
#   infra/scripts/generar-token.sh luis --expirado
#
# Usuarios: ana, luis, marta (ESTUDIANTE), profesor (PROFESOR), pedro (ESTUDIANTE no inscrito).
# Opciones: --minutos <n> (60 por defecto), --expirado, --llave <ruta PEM privada>.
set -euo pipefail

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
uso() { echo "Uso: $0 <ana|luis|marta|profesor|pedro> [--minutos n] [--expirado] [--llave ruta]" >&2; exit 1; }

case "${1:-}" in
  profesor) SUB=a0000000-0000-0000-0000-000000000001 ROL=PROFESOR   NOMBRE="Laura Rincón" ;;
  ana)      SUB=b0000000-0000-0000-0000-000000000001 ROL=ESTUDIANTE NOMBRE="Ana" ;;
  luis)     SUB=b0000000-0000-0000-0000-000000000002 ROL=ESTUDIANTE NOMBRE="Luis" ;;
  marta)    SUB=b0000000-0000-0000-0000-000000000003 ROL=ESTUDIANTE NOMBRE="Marta" ;;
  pedro)    SUB=b0000000-0000-0000-0000-000000000099 ROL=ESTUDIANTE NOMBRE="Pedro (no inscrito)" ;;
  *) uso ;;
esac
shift

MINUTOS=60
LLAVE="$DIR/../keys/jwt-private.pem"
DESFASE=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --minutos) MINUTOS="${2:?}"; shift 2 ;;
    --llave) LLAVE="${2:?}"; shift 2 ;;
    --expirado) DESFASE=7200; shift ;;
    *) uso ;;
  esac
done

[[ -f "$LLAVE" ]] || { echo "No existe $LLAVE. Ejecute primero infra/scripts/generar-llaves.sh" >&2; exit 1; }

b64url() { openssl base64 -A | tr '+/' '-_' | tr -d '='; }

IAT=$(( $(date +%s) - DESFASE ))
EXP=$(( IAT + MINUTOS * 60 ))
ENCABEZADO=$(printf '{"alg":"RS256","typ":"JWT"}' | b64url)
CARGA=$(printf '{"sub":"%s","rol":"%s","nombre":"%s","iss":"unillanos-usuarios","aud":"unillanos-notas","iat":%d,"nbf":%d,"exp":%d}' \
  "$SUB" "$ROL" "$NOMBRE" "$IAT" "$IAT" "$EXP" | b64url)
FIRMA=$(printf '%s.%s' "$ENCABEZADO" "$CARGA" | openssl dgst -sha256 -sign "$LLAVE" -binary | b64url)

echo "$ENCABEZADO.$CARGA.$FIRMA"
