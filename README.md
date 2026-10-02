# Sistema unificado de notas — Universidad de los Llanos (MVP)

Une el campus virtual (Moodle) y el SIAU en microservicios .NET 10, con una base de datos por servicio.

```
Frontend (React) -> API Gateway (YARP, 5000) -> Usuarios (5001) · Evaluaciones y cursos (5002) · Entregas (5003)
                                                SQL Server 2022 (1433) · Azurite / Blob Storage (10000)
```

| Carpeta | Contenido |
|---|---|
| `contracts/` | Contratos OpenAPI: la fuente de verdad de la API. Los cambios propuestos van en `CAMBIOS_CONTRATO.md`. |
| `services/compartido/` | `Unillanos.ServiceDefaults` (errores, JWT, correlación, health, logs) y `Unillanos.Pruebas.Compartidas` (ayudas de prueba). |
| `services/<servicio>/` | `src/` en capas Api / Application / Domain / Infrastructure, `tests/`, `Dockerfile` y `README.md`. |
| `infra/` | `docker-compose.yml`, `.env.example` y scripts de llaves y tokens de desarrollo. |

## Requisitos

- .NET SDK 10 (fijado en `global.json`).
- Docker, para el entorno local y las pruebas de integración.
- Node.js 22, para el frontend (`Frontend/Interfaz/code`).
- `openssl` y `bash`, para los scripts de `infra/scripts`.

## Levantar el entorno local

```bash
cp infra/.env.example infra/.env        # edite los valores; infra/.env no se versiona
infra/scripts/generar-llaves.sh         # llaves JWT de desarrollo en infra/keys (ignorada por git)
docker compose -f infra/docker-compose.yml up --build
```

Para obtener un token de prueba de un usuario semilla (`ana`, `luis`, `marta`, `profesor` o `pedro`):

```bash
TOKEN=$(infra/scripts/generar-token.sh ana)
curl -s http://localhost:5002/mis-notas -H "Authorization: Bearer $TOKEN"
```

Para usar la aplicación web contra este backend (rol estudiante), en otra terminal:

```bash
cd Frontend/Interfaz/code
npm install
npm run dev:backend                     # http://localhost:5173 · usuario E0001, contraseña Demo1234!
```

Los comandos de cada servicio están en su README: [evaluaciones](services/evaluaciones/README.md) y [entregas](services/entregas/README.md).

## Compilar y probar

```bash
dotnet build UnillanosNotas.slnx
dotnet test UnillanosNotas.slnx
```

Las pruebas de integración levantan SQL Server y Azurite con Testcontainers, así que necesitan Docker.
Las migraciones de EF Core se generan con la herramienta local:

```bash
dotnet tool restore
dotnet ef migrations add <Nombre> --output-dir Persistencia/Migraciones \
  --project services/<servicio>/src/<Servicio>.Infrastructure \
  --startup-project services/<servicio>/src/<Servicio>.Infrastructure
```

## Reglas del repositorio

- Ningún secreto, llave ni archivo `.env` va al repositorio.
- Sin llaves foráneas ni consultas entre bases de servicios distintos: las referencias entre servicios son solo ids.
- Toda la configuración sale de variables de entorno con doble guion bajo (`ConnectionStrings__Default`, `Jwt__PublicKeyPath`…).
