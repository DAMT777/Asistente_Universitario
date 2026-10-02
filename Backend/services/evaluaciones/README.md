# Servicio de evaluaciones y cursos

ASP.NET Core sobre .NET 10 con EF Core y SQL Server. Puerto local 5002.

## Casos de uso implementados

| CU | Endpoint | Caso de uso |
|----|----------|-------------|
| CU-09 | `GET /cursos/{cursoId}/ponderado` y `GET /cursos/{cursoId}/ponderado/{estudianteId}` | `ConsultarPonderado` |
| CU-10 | `POST /cursos/{cursoId}/cortes/{corte}/publicar` | `PublicarCorte` |
| CU-11 | `PUT /cursos/{cursoId}/cortes/{corte}/estudiantes/{estudianteId}` | `CorregirCorte` |
| CU-12 | `GET /cursos` y `GET /cursos/{cursoId}/actividades?soloPendientes=true` | `ListarCursos`, `ListarActividadesDelCurso` |

Lecturas de apoyo que la pantalla del profesor necesita para CU-09, CU-10 y CU-11, y que en rigor son del frente B: `GET /cursos/{cursoId}/estudiantes` y `GET /actividades/{actividadId}/calificaciones`.

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

En desarrollo no hace falta SQL Server: `appsettings.Development.json` activa `Database:UseInMemory`, que usa una base en memoria y carga los datos semilla de la guía (sección 9.8) al arrancar. Los datos se pierden al reiniciar. En cualquier otro entorno se usa SQL Server con la cadena de conexión.

| Variable | Valor local de ejemplo |
|----------|------------------------|
| `Database__UseInMemory` | `true` (solo desarrollo) |
| `ConnectionStrings__Default` | `Server=localhost,1433;Database=evaluaciones_db;User Id=sa;Password=<clave>;TrustServerCertificate=True` |
| `Jwt__PublicKeyPath` | `../../../../../infra/keys/jwt-public.pem` (ya viene en `appsettings.Development.json`) |
| `Jwt__Issuer` / `Jwt__Audience` | `unillanos-usuarios` / `unillanos-notas` |

La cadena de conexión con contraseña va en `dotnet user-secrets` o en una variable de entorno, nunca en un archivo versionado:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=evaluaciones_db;User Id=sa;Password=<clave>;TrustServerCertificate=True" --project src/Evaluaciones.Api
```

## Comandos

```bash
dotnet test Evaluaciones.slnx                        # 65 unitarias + 23 de integración
dotnet run --project src/Evaluaciones.Api            # levanta en http://localhost:5002
```

## Decisiones que conviene alinear con el equipo

- **Ponderado del profesor (CU-09).** Incluye las calificaciones en borrador (sirven para la "nota fantasma") y las marca con `incluyeBorradores`. La definitiva parcial suma los cortes que ya tienen alguna actividad calificada. El corte sin calificaciones sale con nota `null`, nunca 0.
- **Corregir un corte (CU-11).** Aplica la regla RN-12: solo cuenta lo publicado y rechaza si hay borradores. Como `PUT /actividades/{id}/calificaciones/{estudianteId}` deja la nota en borrador, el profesor tiene que publicar la actividad (CU-08) antes de corregir el corte. Si el frente B decide que modificar una calificación ya publicada la deja publicada, el flujo se acorta sin tocar este código.
- **Publicar de nuevo (CU-10).** Si el estudiante ya tiene publicación en ese corte, se actualiza la misma fila en lugar de duplicarla.
- **Actividad pendiente (CU-12).** Significa sin calificación publicada. Si el estudiante ya entregó lo sabe el servicio de entregas, y el frontend combina ambas respuestas con `/mis-entregas`.
- **Redondeo (DA-01).** A un decimal, mitad hacia arriba, al publicar la nota del corte y al mostrar la definitiva.

## Pendiente de otros frentes

Migraciones de EF Core para SQL Server (frente B), el resto de endpoints de cursos, actividades y calificaciones, CU-15 y CU-16 (`/mis-notas`), y el contrato `contracts/evaluaciones.yaml`.
