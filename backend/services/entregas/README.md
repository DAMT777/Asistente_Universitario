# Servicio de entregas

ASP.NET Core sobre .NET 10 con EF Core y SQL Server. Puerto local 5003.

## Casos de uso implementados

| CU | Endpoint | Rol | Caso de uso |
|----|----------|-----|-------------|
| CU-04 | `GET /actividades/{actividadId}/entregas` | Profesor dueño del curso | `ListarEntregasDeActividad` |
| — | `GET /mis-entregas?actividadId=` | Estudiante | `ListarMisEntregas` |
| — | `GET /entregas/{entregaId}/archivo` | Profesor dueño o estudiante autor | `DescargarArchivo` |

Pendiente: subir, reemplazar y anular (CU-13 y CU-14) y el almacenamiento en Blob Storage.

## Estructura

```
src/
  Entregas.Domain/          Entrega, estados y errores de dominio
  Entregas.Application/     casos de uso, DTO y puertos (repositorio, almacén de archivos, cliente de Evaluaciones)
  Entregas.Infrastructure/  EF Core (entregas_db), almacén en disco local y cliente HTTP de Evaluaciones
  Entregas.Api/             controlador, JWT y middleware de errores
tests/
  Entregas.UnitTests/       casos de uso con dobles en memoria
```

## Reglas

- **Propiedad del curso (RN-15).** El servicio no conoce los cursos: antes de listar o descargar le pregunta a Evaluaciones por `GET /internal/actividades/{id}` (con `X-Service-Key`) quién es el profesor. Verificar solo el rol no basta: otro profesor recibe 403.
- **Si Evaluaciones no responde** (red, 5 s de espera o error 5xx) responde 503 `SERVICIO_NO_DISPONIBLE` y nunca asume que la actividad es válida.
- **Privacidad (RN-16).** Un estudiante solo ve y descarga sus entregas; para él las ajenas no existen (404). Una entrega anulada ya no se descarga.
- **Archivos (sección 12.5).** El contenedor es privado y las descargas pasan siempre por el servicio. La ruta interna es `{actividadId}/{estudianteId}/{guid}-{nombre}` y nunca sale en las respuestas.
- El profesor ve también las entregas anuladas, con su estado; el frontend muestra solo las enviadas.

## Configuración

| Variable | Valor local de ejemplo |
|----------|------------------------|
| `Database__UseInMemory` | `true` (solo desarrollo, carga cuatro entregas de demostración) |
| `ConnectionStrings__Default` | `Server=localhost,1433;Database=entregas_db;User Id=sa;Password=<clave>;TrustServerCertificate=True` |
| `Jwt__PublicKeyPath` | `../../../../../infra/keys/jwt-public.pem` |
| `Services__EvaluacionesBaseUrl` | `http://localhost:5002` |
| `ServiceKey` | La misma de Evaluaciones |
| `Storage__LocalPath` | `almacen-entregas` (disco local mientras no se conecta Blob Storage) |

## Comandos

```bash
dotnet test Entregas.slnx                       # 12 unitarias
dotnet run --project src/Entregas.Api           # levanta en http://localhost:5003
```
