# Servicio de evaluaciones y cursos (.NET 10)

Casos de uso del profesor sobre calificaciones, implementados según la guía técnica del MVP:

| CU | Qué hace | Endpoint |
| --- | --- | --- |
| CU-05 | Calificar una entrega con nota y retroalimentación | `PUT /actividades/{id}/calificaciones/{estudianteId}` con `entregaId` |
| CU-06 | Registrar la nota de una actividad sin entrega (parcial, sustentación) | el mismo `PUT`, sin `entregaId` |
| CU-07 | Modificar una nota ya subida (reclamo) | el mismo `PUT` sobre una calificación existente |
| CU-08 | Publicar las calificaciones de una actividad | `POST /actividades/{id}/calificaciones/publicar` |

Además, `GET /actividades/{id}/calificaciones` lista las notas (con su versión) para el profesor dueño.
El contrato está en `../../contracts/evaluaciones.yaml`.

## Reglas que implementa

- Solo el profesor **dueño del curso** califica, modifica y publica (RN-15). Estudiante o profesor ajeno: `403 SIN_PERMISO`.
- Escala 0.0 a 5.0 con un decimal: fuera de rango `422 NOTA_FUERA_DE_RANGO`; más de un decimal `400 VALIDACION_FALLIDA`.
- `valor` es obligatorio. Un `0.0` es una nota; sin calificar significa que no existe la fila (RN-08).
- Toda calificación nueva o modificada queda en **BORRADOR**. Al modificar una ya publicada vuelve a borrador y el estudiante deja de verla hasta que el profesor publique la actividad otra vez. Si la modificación no cambia nada, el estado no se toca.
- `entregaId` solo se acepta si la actividad requiere entrega. Si se omite al modificar, se conserva el vínculo actual.
- El estudiante debe estar inscrito en el curso (`404 NO_ENCONTRADO` si no).
- Concurrencia: `If-Match` con la `version` leída. Si cambió, `409 CONFLICTO_CONCURRENCIA` (también si dos altas chocan).
- Publicar es idempotente y atómico. Extensión compatible con el contrato: el cuerpo opcional `{ "estudianteIds": [...] }` publica solo esas notas (lo usa el botón "Publicar" de cada tarjeta del frontend).

## Estructura

```
src/Evaluaciones.Domain          Calificacion (reglas), Actividad, Curso, errores con código
src/Evaluaciones.Application     GuardarCalificacion, PublicarCalificaciones, ListarCalificaciones, interfaces
src/Evaluaciones.Infrastructure  EF Core (SQL Server), repositorios, datos semilla
src/Evaluaciones.Api             Controller, JWT RS256, manejo de errores, health checks
tests/Evaluaciones.UnitTests     xUnit con repositorios en memoria (48 pruebas)
http/calificaciones.http         recorrido manual
```

## Cómo correrlo

```bash
# desde code/
bash infra/scripts/generar-token-dev.sh          # crea infra/keys/ la primera vez e imprime un JWT de profesor
cd services/evaluaciones
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=evaluaciones_db;User Id=sa;Password=<clave>;TrustServerCertificate=True" --project src/Evaluaciones.Api
dotnet ef migrations add EsquemaInicial --project src/Evaluaciones.Infrastructure --startup-project src/Evaluaciones.Api
dotnet ef database update --project src/Evaluaciones.Infrastructure --startup-project src/Evaluaciones.Api
dotnet run --project src/Evaluaciones.Api          # http://localhost:5002, siembra datos en Development
dotnet test
```

La migración `EsquemaInicial` hay que generarla una vez y subirla al repositorio. Los datos semilla son los de la guía 9.8
(profesor `a0000000-…-01`, Ana, Luis y Marta, curso 603803 con Taller 1, Parcial 1 y Proyecto 1; Ana con notas publicadas y Luis con un borrador).

## Fuera de este alcance

`PublicacionCorte` (CU-10 y CU-11), el motor de ponderado, la creación de cursos y actividades, y las vistas del estudiante.
Ojo: si se modifica y se republica una nota después de haber publicado el corte, la `PublicacionCorte` de ese estudiante no se actualiza sola; eso lo hace CU-11.
