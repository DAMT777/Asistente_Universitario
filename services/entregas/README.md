# Servicio de Entregas (puerto 5003)

CU-13 y CU-14 del rol ESTUDIANTE: subir, editar, anular y listar las entregas propias.
Contrato: [`contracts/entregas.yaml`](../../contracts/entregas.yaml). Base de datos propia `entregas_db`; los archivos van a Blob Storage (Azurite en local), nunca a la base.

| Método | Ruta | Qué hace |
|---|---|---|
| POST | `/actividades/{actividadId}/entregas` | Sube la entrega (multipart, campo `archivo`). 201. Si ya existe, reemplaza el archivo en la misma fila. |
| PUT | `/entregas/{entregaId}` | Reemplaza el archivo mientras no venza la fecha. 200. |
| DELETE | `/entregas/{entregaId}` | Anula la entrega mientras no venza la fecha. 200 con estado `ANULADA`. |
| GET | `/mis-entregas?actividadId=` | Lista las entregas propias (filtro opcional). |
| GET | `/health/live`, `/health/ready` | Salud (ready revisa SQL Server y Blob Storage). |

Para cada subida, edición o anulación consulta a Evaluaciones (`GET /internal/actividades/{id}?estudianteId=`, con `X-Service-Key`, timeout de 3 s y sin reintentos). Si Evaluaciones no responde, devuelve 503 `SERVICIO_NO_DISPONIBLE`.

## Capas

- `Unillanos.Entregas.Domain`: `Entrega`, `PlazoEntrega` (fecha límite inclusiva, en UTC), `PoliticaSubida`, `ValidadorArchivo` (extensión y firma). Sin dependencias.
- `Unillanos.Entregas.Application`: casos de uso `SubirEntrega`, `EditarEntrega`, `AnularEntrega`, `ListarMisEntregas`; puertos `IEntregaRepositorio`, `IAlmacenArchivos`, `IEvaluacionesClient`.
- `Unillanos.Entregas.Infrastructure`: EF Core (migraciones en `Persistencia/Migraciones`), `BlobAlmacenArchivos` y `EvaluacionesHttpClient`.
- `Unillanos.Entregas.Api`: controlador delgado y composición. Los errores se traducen en `services/compartido/Unillanos.ServiceDefaults`.

## Variables de entorno

| Variable | Ejemplo / valor por defecto |
|---|---|
| `ConnectionStrings__Default` | `Server=localhost,1433;Database=entregas_db;User Id=sa;Password=...;TrustServerCertificate=True` |
| `Jwt__PublicKeyPath` | `/keys/jwt-publica.pem` |
| `Jwt__Issuer` / `Jwt__Audience` | `unillanos-usuarios` / `unillanos-notas` |
| `ServiceKey` | Llave compartida con Evaluaciones (mín. 16 caracteres) |
| `Services__EvaluacionesBaseUrl` | `http://localhost:5002` |
| `Services__EvaluacionesTimeoutSegundos` | `3` |
| `Storage__ConnectionString` | `UseDevelopmentStorage=true` (Azurite local) |
| `Storage__Container` | `entregas` |
| `Entregas__MaxBytes` | `20971520` (20 MB) |
| `Database__AplicarMigraciones` | `true` en Development; `false` por defecto |

Entregas no tiene datos semilla: una entrega solo existe si su archivo se subió.

## Correrlo

Con Docker (desde la raíz del repositorio), incluyendo SQL Server, Azurite y Evaluaciones:

```bash
cp infra/.env.example infra/.env   # edite los valores
infra/scripts/generar-llaves.sh
docker compose -f infra/docker-compose.yml up --build
```

Sin Docker para el servicio (SQL Server, Azurite y Evaluaciones ya arriba):

```bash
export ConnectionStrings__Default="Server=localhost,1433;Database=entregas_db;User Id=sa;Password=<clave>;TrustServerCertificate=True"
export Jwt__PublicKeyPath="$PWD/infra/keys/jwt-publica.pem"
export ServiceKey="<la misma de Evaluaciones>"
export Services__EvaluacionesBaseUrl="http://localhost:5002"
export Storage__ConnectionString="UseDevelopmentStorage=true"
dotnet run --project services/entregas/src/Unillanos.Entregas.Api
```

## Probarlo con curl

```bash
TOKEN=$(infra/scripts/generar-token.sh ana)
printf '%%PDF-1.7\nmi taller\n' > /tmp/taller.pdf

# CU-13: Taller 1 (abierta) -> 201 ENVIADA
curl -s -X POST http://localhost:5003/actividades/d0000000-0000-0000-0000-000000000001/entregas \
  -H "Authorization: Bearer $TOKEN" -F "archivo=@/tmp/taller.pdf"

# Proyecto 1 (vencido) -> 422 FECHA_LIMITE_VENCIDA; Parcial 1 (sin entrega) -> 422 ACTIVIDAD_SIN_ENTREGA
curl -s -X POST http://localhost:5003/actividades/d0000000-0000-0000-0000-000000000003/entregas \
  -H "Authorization: Bearer $TOKEN" -F "archivo=@/tmp/taller.pdf"

# Mis entregas
curl -s http://localhost:5003/mis-entregas -H "Authorization: Bearer $TOKEN"

# CU-14: editar y anular (use el id que devolvió la subida)
curl -s -X PUT http://localhost:5003/entregas/<entregaId> -H "Authorization: Bearer $TOKEN" -F "archivo=@/tmp/taller.pdf"
curl -s -X DELETE http://localhost:5003/entregas/<entregaId> -H "Authorization: Bearer $TOKEN"
```

Otros tokens: `generar-token.sh luis|marta|pedro|profesor`, `--expirado` y `--minutos n`.

## Pruebas

```bash
dotnet test services/entregas/tests/Unillanos.Entregas.UnitTests
dotnet test services/entregas/tests/Unillanos.Entregas.IntegrationTests   # requiere Docker
```

Las pruebas de integración levantan SQL Server 2022 real y Azurite con Testcontainers, usan WireMock para simular Evaluaciones (timeout, caída, 500) y validan las respuestas contra `contracts/entregas.yaml`.
