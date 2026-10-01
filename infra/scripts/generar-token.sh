#!/usr/bin/env bash
# Atajo: TOKEN=$(infra/scripts/generar-token.sh ana)
set -euo pipefail
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec dotnet run "$DIR/generar-token.cs" -- "$@"
