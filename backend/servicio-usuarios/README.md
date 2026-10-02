# Servicio de Usuarios (autenticación) — MVP

API de autenticación basada en `mini-identity-api-dotnet`, adaptada para guardar los datos en **PostgreSQL**.
Arquitectura en capas: `Domain` → `Application` → `Infrastructure` → `Api`.

## Ejecutar todo con Docker
```bash
cp .env.example .env        # (Windows PowerShell: Copy-Item .env.example .env) y edita las claves
docker compose up -d --build
```
- API + Swagger: http://localhost:5001/swagger
- Health: http://localhost:5001/health
- PostgreSQL desde tu PC: `localhost:5433` (db `usuarios`)

## Ejecutar la API local (solo la BD en Docker)
```bash
docker compose up -d db-usuarios
dotnet run --project src/MiniIdentityApi.Api     # http://localhost:5132/swagger
```

## Endpoints principales
| Método | Ruta | Acceso |
|---|---|---|
| POST | `/api/auth/login` | Público |
| POST | `/api/auth/register` | Público |
| GET | `/api/demo/profile` | Token válido |
| GET | `/api/users`, `/api/users/{id}` | Admin / token |

## Usuarios semilla
| Usuario | Contraseña | Rol | Id fijo |
|---|---|---|---|
| admin | Admin123* | Admin | (aleatorio) |
| lrincon | Pass123* | PROFESOR | 11111111-1111-1111-1111-111111111111 |
| 160005017 | Pass123* | ESTUDIANTE | 22222222-2222-2222-2222-222222222222 |
| 160005021 | Pass123* | ESTUDIANTE | 33333333-3333-3333-3333-333333333333 |

El token incluye los claims `sub` (id), `role` y `nombre`. Los otros servicios lo validan con el mismo `Jwt:Key`, `Issuer` y `Audience`.
