# Sistema unificado de notas — Universidad de los Llanos (MVP)

Une el campus virtual (Moodle) y el SIAU en microservicios .NET 10, con una base de datos por servicio.

```
Frontend (React, 5173) -> API Gateway (YARP, 5000) -> Usuarios (5001) · Evaluaciones y cursos (5002) · Entregas (5003)
                                                       SQL Server 2022 (1433) · Azurite / Blob Storage (10000)
```

| Carpeta | Contenido |
|---|---|
| `Frontend/Interfaz/code/` | Aplicación web (React + Vite). Entra al backend solo por el gateway. |
| `services/gateway/` | Punto único de entrada (YARP): valida el JWT, aplica CORS, bloquea `/internal/**` y enruta. |
| `services/usuarios/` | Login (CU-01) contra `usuarios_db` (EF Core; en desarrollo, base en memoria con los usuarios de prueba), `/auth/me` y emisión del JWT RS256. |
| `services/evaluaciones/` | Cursos, pesos de los cortes y actividades (CU-02, CU-03), calificaciones de actividad (CU-05 a CU-08), ponderado y publicación de cortes (CU-09 a CU-12) y notas del estudiante (CU-15, CU-16). |
| `services/entregas/` | Entregas con uno o varios archivos en Blob Storage (CU-13, CU-14) y su consulta por el profesor. |
| `services/compartido/` | `Unillanos.ServiceDefaults` (errores, JWT, correlación, health, logs) y `Unillanos.Pruebas.Compartidas` (ayudas de prueba y lector de contratos). |
| `contracts/` | Contratos OpenAPI: la fuente de verdad de la API. |
| `infra/` | `docker-compose.yml`, `.env.example` y scripts de llaves y tokens de desarrollo. |

Cada servicio tiene `src/` en capas Api / Application / Domain / Infrastructure, `tests/`, `Dockerfile` y su `README.md`.

## Requisitos

- .NET SDK 10 (fijado en `global.json`).
- Docker, para el entorno local y las pruebas de integración.
- Node.js 22, para el frontend.
- `openssl` y `bash`, para los scripts de `infra/scripts` (en Windows, `generar-llaves.ps1`).

## Levantar todo

```bash
cp infra/.env.example infra/.env        # edite los valores; infra/.env no se versiona
infra/scripts/generar-llaves.sh         # llaves JWT de desarrollo en infra/keys (ignorada por git)
docker compose -f infra/docker-compose.yml up --build
```

Y el frontend, en otra terminal, apuntando al gateway:

```bash
cd Frontend/Interfaz/code
npm install
VITE_API_URL=http://localhost:5000 npm run dev      # http://localhost:5173
```

Sin `VITE_API_URL` el frontend usa su api simulada en memoria (no necesita backend).

Usuarios de prueba (contraseña `Demo1234!`, solo existen en desarrollo):

| Código | Rol | Para probar |
|--------|-----|-------------|
| `P0001` | PROFESOR | Cursos, calificar y publicar notas de actividad, ponderado, publicar y corregir cortes, ver y descargar las entregas |
| `E0001` | ESTUDIANTE | Ana: corte 1 publicado con 3.2 y definitiva parcial 1.0 |
| `E0002` | ESTUDIANTE | Luis: tiene un borrador que no debe ver |
| `E0003` | ESTUDIANTE | Marta: sin notas; entrega, reemplaza, anula y descarga en Taller 1 |

### Sin Docker para los servicios

Evaluaciones puede correr con una base en memoria (`Database__UseInMemory=true`, ya activa en `appsettings.Development.json`). Entregas necesita SQL Server y Azurite (por ejemplo `docker compose -f infra/docker-compose.yml up -d sqlserver azurite`). Cada servicio en su terminal, desde la raíz:

```bash
dotnet run --project services/usuarios/src/Usuarios.Api
ServiceKey=<llave> dotnet run --project services/evaluaciones/src/Evaluaciones.Api
dotnet run --project services/gateway/src/Gateway.Api
```

Entregas con dotnet run: ver [services/entregas/README.md](services/entregas/README.md).

### Probar la API con curl

Por el gateway, con login real:

```bash
TOKEN=$(curl -s -X POST localhost:5000/auth/login -H 'Content-Type: application/json' -d '{"usuario":"E0001","password":"Demo1234!"}' | python3 -c 'import sys,json;print(json.load(sys.stdin)["accessToken"])')
curl -s localhost:5000/mis-notas -H "Authorization: Bearer $TOKEN"
```

O con un token firmado localmente (`ana`, `luis`, `marta`, `pedro` o `profesor`): `TOKEN=$(infra/scripts/generar-token.sh ana)`.

## Compilar y probar

```bash
dotnet build UnillanosNotas.slnx
dotnet test UnillanosNotas.slnx
cd Frontend/Interfaz/code && npm test && npm run typecheck && npm run lint
```

Las pruebas de integración de Entregas y la de migraciones de Evaluaciones levantan SQL Server y Azurite con Testcontainers, así que necesitan Docker. Las migraciones de EF Core se generan con la herramienta local:

```bash
dotnet tool restore
dotnet ef migrations add <Nombre> --output-dir Persistencia/Migraciones \
  --project services/<servicio>/src/<Servicio>.Infrastructure \
  --startup-project services/<servicio>/src/<Servicio>.Infrastructure
```

## Qué funciona de punta a punta

| Pantalla | Estado |
|----------|--------|
| Login de profesor y estudiante | Funciona (servicio de usuarios) |
| Profesor: inicio, cursos, detalle, ponderado y notas por corte (CU-09 a CU-12) | Funciona |
| Profesor: ver y descargar las entregas de cada estudiante | Funciona |
| Estudiante: inicio, cursos, notas y matriz por corte (CU-12, CU-15, CU-16) | Funciona |
| Estudiante: entregar uno o varios archivos, reemplazar, anular y descargar (CU-13, CU-14) | Funciona |
| Profesor: calificar, retroalimentar, modificar y publicar notas de actividad (CU-05 a CU-08) | Funciona |
| Profesor: pesos de los cortes y crear o editar actividades (CU-02, CU-03) | Funciona |
| Profesor: filtrar *Con entrega / Sin entrega* al calificar (CU-04) | Funciona |
| Sesión vencida: el frontend cierra la sesión y vuelve al acceso con un aviso | Funciona |

## Reglas del repositorio

- Ningún secreto, llave ni archivo `.env` va al repositorio.
- Sin llaves foráneas ni consultas entre bases de servicios distintos: las referencias entre servicios son solo ids.
- Toda la configuración sale de variables de entorno con doble guion bajo (`ConnectionStrings__Default`, `Jwt__PublicKeyPath`…).
