# Servicio de Evaluaciones y cursos (puerto 5002) — parte del rol ESTUDIANTE

CU-15 (nota y retroalimentación por actividad) y CU-16 (matriz de notas con cortes y definitiva parcial), más la consulta interna que usa Entregas.
Contrato: [`contracts/evaluaciones.yaml`](../../contracts/evaluaciones.yaml). Base de datos propia `evaluaciones_db`.

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/mis-notas` | Matriz por curso inscrito: cortes (nota publicada o `null`), `definitivaParcial` y `esParcial`. |
| GET | `/mis-notas/cursos/{cursoId}/actividades` | Cada actividad del curso con su nota `PUBLICADA` o `SIN_CALIFICAR` (nota `null`). |
| GET | `/internal/actividades/{id}?estudianteId=` | Solo servicio a servicio, con `X-Service-Key`. El gateway debe bloquear `/internal/**`. |
| GET | `/health/live`, `/health/ready` | Salud (ready revisa SQL Server). |

Reglas: el estudiante solo ve calificaciones `PUBLICADAS`; un borrador o una actividad sin calificar se muestran igual (`SIN_CALIFICAR`, nota `null`, nunca 0). La nota de cada corte sale de `PublicacionCorte` (última versión). La definitiva parcial se calcula en `Domain/Ponderado/CalculadoraPonderado.cs` y nunca se guarda. El redondeo de presentación (1 decimal, `AwayFromZero`, DA-01) está solo en `Domain/Ponderado/PoliticaRedondeo.cs`.

## Variables de entorno

| Variable | Ejemplo / valor por defecto |
|---|---|
| `ConnectionStrings__Default` | `Server=localhost,1433;Database=evaluaciones_db;User Id=sa;Password=...;TrustServerCertificate=True` |
| `Jwt__PublicKeyPath` | `/keys/jwt-publica.pem` |
| `Jwt__Issuer` / `Jwt__Audience` | `unillanos-usuarios` / `unillanos-notas` |
| `ServiceKey` | Llave que debe enviar Entregas en `X-Service-Key` (mín. 16 caracteres) |
| `Database__AplicarMigraciones` | `true` en Development; `false` por defecto |

Con `ASPNETCORE_ENVIRONMENT=Development` se cargan los datos semilla (idempotentes, GUID fijos): curso 603803 (30/30/40), Taller 1 (abierta), Parcial 1 (sin entrega), Proyecto 1 (vencido); Ana con 4.0 y 3.0 publicados y corte 1 = 3.2; Luis con un borrador de 2.5; Marta sin notas. Las fechas límite son relativas al momento de la primera carga.

## Correrlo

Con Docker: ver `infra/docker-compose.yml` (lo levanta junto con Entregas, SQL Server y Azurite).

Sin Docker para el servicio (SQL Server ya arriba):

```bash
export ConnectionStrings__Default="Server=localhost,1433;Database=evaluaciones_db;User Id=sa;Password=<clave>;TrustServerCertificate=True"
export Jwt__PublicKeyPath="$PWD/infra/keys/jwt-publica.pem"
export ServiceKey="<llave compartida>"
dotnet run --project services/evaluaciones/src/Unillanos.Evaluaciones.Api
```

## Probarlo con curl

```bash
# E-13: Ana -> corte 1 = 3.2, definitivaParcial = 1.0, esParcial = true
curl -s http://localhost:5002/mis-notas -H "Authorization: Bearer $(infra/scripts/generar-token.sh ana)"

# E-14: Marta -> cortes null/false, definitivaParcial = 0.0
curl -s http://localhost:5002/mis-notas -H "Authorization: Bearer $(infra/scripts/generar-token.sh marta)"

# E-07: Luis -> Taller 1 SIN_CALIFICAR con nota null (no ve su borrador 2.5)
curl -s http://localhost:5002/mis-notas/cursos/c0000000-0000-0000-0000-000000000001/actividades \
  -H "Authorization: Bearer $(infra/scripts/generar-token.sh luis)"

# No inscrito -> 403 SIN_PERMISO
curl -s http://localhost:5002/mis-notas/cursos/c0000000-0000-0000-0000-000000000001/actividades \
  -H "Authorization: Bearer $(infra/scripts/generar-token.sh pedro)"

# Interno (como lo llama Entregas)
curl -s "http://localhost:5002/internal/actividades/d0000000-0000-0000-0000-000000000001?estudianteId=b0000000-0000-0000-0000-000000000001" \
  -H "X-Service-Key: <ServiceKey>"
```

## Pruebas

```bash
dotnet test services/evaluaciones/tests/Unillanos.Evaluaciones.UnitTests
dotnet test services/evaluaciones/tests/Unillanos.Evaluaciones.IntegrationTests   # requiere Docker
```

Las pruebas de integración usan SQL Server 2022 real (Testcontainers), cargan la semilla de Development y validan las respuestas contra `contracts/evaluaciones.yaml`.
