# Servicio de evaluaciones y cursos

ASP.NET Core sobre .NET 10 con EF Core y SQL Server. Puerto local 5002.

## Casos de uso implementados

| CU | Endpoint | Caso de uso |
|----|----------|-------------|
| — | `GET /cursos/{cursoId}` (profesor dueño o estudiante inscrito) | `ObtenerCurso` |
| CU-02 | `PUT /cursos/{cursoId}/pesos` (profesor) | `DefinirPesosCortes` |
| CU-03 | `POST /cursos/{cursoId}/actividades` y `PUT /actividades/{actividadId}` (profesor) | `GestionarActividad` |
| CU-05, CU-06, CU-07 | `PUT /actividades/{actividadId}/calificaciones/{estudianteId}` (profesor; `If-Match` opcional) | `CalificarActividad` |
| CU-08 | `POST /actividades/{actividadId}/calificaciones/publicar` (profesor) | `PublicarCalificacionesDeActividad` |
| CU-09 | `GET /cursos/{cursoId}/ponderado` y `GET /cursos/{cursoId}/ponderado/{estudianteId}` | `ConsultarPonderado` |
| CU-10 | `POST /cursos/{cursoId}/cortes/{corte}/publicar` | `PublicarCorte` |
| CU-11 | `PUT /cursos/{cursoId}/cortes/{corte}/estudiantes/{estudianteId}` | `CorregirCorte` |
| CU-12 | `GET /cursos` y `GET /cursos/{cursoId}/actividades?soloPendientes=true` | `ListarCursos`, `ListarActividadesDelCurso` |
| CU-15 | `GET /mis-notas/cursos/{cursoId}/actividades` (estudiante) | `ObtenerNotasActividades` |
| CU-16 | `GET /mis-notas` (estudiante) | `ObtenerMatrizNotas` |

CU-02 y CU-03 vienen de la rama `Angy`. Los pesos de los tres cortes deben sumar 100, cada uno entre 0 y 100 con dos decimales como máximo (422 `PESOS_CORTE_INVALIDOS`). Una actividad necesita título, corte 1 a 3 y un peso mayor que 0; las del mismo corte no pueden pasar de 100 (422 `PESOS_ACTIVIDAD_EXCEDIDOS`). Si requiere entrega, la fecha límite es obligatoria, y una fecha nueva debe ser futura (al editar se puede conservar la que ya tenía). El ponderado se calcula al consultar, así que un cambio de pesos se refleja de inmediato.

CU-05 a CU-08 vienen de la rama `velez`. Calificar crea o modifica la nota y la retroalimentación del estudiante; toda nota nueva o modificada queda en `BORRADOR` y el estudiante no la ve hasta que se publica la actividad (CU-08), que pasa a `PUBLICADA` todos los borradores de esa actividad y responde cuántos publicó. Si la modificación no cambia nada, la calificación conserva su estado. La respuesta lleva `ETag`; si el cliente manda `If-Match` con otra versión recibe 409 `CONFLICTO_CONCURRENCIA`. Nota fuera de 0.0–5.0: 422 `NOTA_FUERA_DE_RANGO`; `entregaId` en una actividad sin entrega: 422 `ACTIVIDAD_SIN_ENTREGA`.

CU-15 y CU-16 siguen `contracts/evaluaciones.yaml`: solo se leen calificaciones PUBLICADAS (un borrador o una actividad sin calificar salen como `SIN_CALIFICAR` con nota `null`), la nota de cada corte sale de `PublicacionCorte` y la definitiva parcial se calcula con `MotorPonderado` sobre los cortes publicados. Curso inexistente: 404; estudiante no inscrito: 403.

Consulta interna para el servicio de entregas: `GET /internal/actividades/{actividadId}?estudianteId=` con el encabezado `X-Service-Key` (variable `ServiceKey`). Devuelve curso, profesor dueño, fecha límite (puede ser `null`), si requiere entrega y si el estudiante está inscrito. Sin llave configurada rechaza todo, y el gateway bloquea `/internal/**` desde fuera.

Lecturas de apoyo que la pantalla del profesor necesita: `GET /cursos/{cursoId}/estudiantes` y `GET /actividades/{actividadId}/calificaciones`.

## Estructura

```
src/
  Evaluaciones.Domain/          entidades, MotorPonderado y excepciones de dominio
  Evaluaciones.Application/     casos de uso, DTO e interfaces de repositorio
  Evaluaciones.Infrastructure/  DbContext de EF Core y repositorios
  Evaluaciones.Api/             controladores, JWT y middleware de errores
tests/
  Evaluaciones.UnitTests/         casos de uso y motor de ponderado, con repositorios en memoria
  Evaluaciones.IntegrationTests/  la API real: JWT, roles, EF Core y casos de uso
```

## Configuración

Mismos nombres de variable que la sección 10.5 de la guía técnica.

En desarrollo no hace falta SQL Server: `appsettings.Development.json` activa `Database:UseInMemory`, que usa una base en memoria y carga los datos semilla de la guía (sección 9.8) al arrancar. Los datos se pierden al reiniciar. En cualquier otro entorno se usa SQL Server con la cadena de conexión: el esquema sale de las migraciones (`src/Evaluaciones.Infrastructure/Persistencia/Migraciones`), que se aplican al arrancar con `Database__AplicarMigraciones=true`, y en Development se carga la misma semilla.

| Variable | Valor local de ejemplo |
|----------|------------------------|
| `Database__UseInMemory` | `true` (solo desarrollo) |
| `ConnectionStrings__Default` | `Server=localhost,1433;Database=evaluaciones_db;User Id=sa;Password=<clave>;TrustServerCertificate=True` |
| `Jwt__PublicKeyPath` | `../../../../infra/keys/jwt-public.pem` (ya viene en `appsettings.Development.json`) |
| `Jwt__Issuer` / `Jwt__Audience` | `unillanos-usuarios` / `unillanos-notas` |
| `Database__AplicarMigraciones` | `true` para crear o actualizar el esquema en SQL Server al arrancar |
| `ServiceKey` | Llave compartida con Entregas para `/internal/**` (mínimo 16 caracteres) |

La cadena de conexión con contraseña va en `dotnet user-secrets` o en una variable de entorno, nunca en un archivo versionado:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=evaluaciones_db;User Id=sa;Password=<clave>;TrustServerCertificate=True" --project src/Evaluaciones.Api
```

## Comandos

```bash
dotnet test Evaluaciones.slnx                        # unitarias + integración (una usa SQL Server con Docker)
dotnet run --project src/Evaluaciones.Api            # levanta en http://localhost:5002
```

## Decisiones que conviene alinear con el equipo

- **Ponderado del profesor (CU-09).** Incluye las calificaciones en borrador (sirven para la "nota fantasma") y las marca con `incluyeBorradores`. La definitiva parcial suma los cortes que ya tienen alguna actividad calificada. El corte sin calificaciones sale con nota `null`, nunca 0.
- **Corregir un corte (CU-11).** Aplica la regla RN-12: solo cuenta lo publicado y rechaza si hay borradores. Como `PUT /actividades/{id}/calificaciones/{estudianteId}` deja la nota en borrador, el profesor tiene que publicar la actividad (CU-08) antes de corregir el corte. Si el frente B decide que modificar una calificación ya publicada la deja publicada, el flujo se acorta sin tocar este código.
- **Publicar de nuevo (CU-10).** Si el estudiante ya tiene publicación en ese corte, se actualiza la misma fila en lugar de duplicarla.
- **Actividad pendiente (CU-12).** Significa sin calificación publicada. Si el estudiante ya entregó lo sabe el servicio de entregas, y el frontend combina ambas respuestas con `/mis-entregas`.
- **Redondeo (DA-01).** A un decimal, mitad hacia arriba, al publicar la nota del corte y al mostrar la definitiva.

## Pendiente de otros frentes

Los endpoints de CU-02, CU-03 y CU-05 a CU-08 y `GET /cursos/{cursoId}` todavía no están en `contracts/evaluaciones.yaml`.
