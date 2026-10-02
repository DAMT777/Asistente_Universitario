# Genera el par de llaves RSA de desarrollo para firmar y verificar el JWT (guía técnica, sección 10.4).
# Cada integrante genera el suyo. infra/keys/ está en el .gitignore y nunca se sube.
$ErrorActionPreference = 'Stop'
$keys = Join-Path (Split-Path $PSScriptRoot -Parent) 'keys'
New-Item -ItemType Directory -Force $keys | Out-Null
openssl genrsa -out (Join-Path $keys 'jwt-private.pem') 2048
openssl rsa -in (Join-Path $keys 'jwt-private.pem') -pubout -out (Join-Path $keys 'jwt-public.pem')
Write-Host 'Llaves generadas en infra/keys/'
