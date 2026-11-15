# Asistente IA: estado del repositorio y plan

Fecha de la inspección: 2026-11-14. Rama `main` (último commit `81f9620`).

## 1. Servicios existentes

| Servicio | Puerto | Proyectos | Base | Usa `Unillanos.ServiceDefaults` |
|---|---|---|---|---|
| Usuarios | 5001 | `Usuarios.Api` (un solo proyecto, sin capas) | `usuarios_db` con EF, **en memoria** en Development (`appsettings.Development.json`, `Database:UseInMemory=true`); docker-compose no le pasa cadena SQL | No (JWT y errores propios en `Program.cs`) |
| Evaluaciones y cursos | 5002 | Api / Application / Domain / Infrastructure | `evaluaciones_db` (migración `Inicial`), semilla en `SemillaDesarrollo.cs` | No (middleware de errores y JWT propios) |
| Entregas | 5003 | Api / Application / Domain / Infrastructure | `entregas_db` + Azurite (contenedor `entregas`) | Sí |
| Gateway YARP | 5000 | `Gateway.Api` | — | No |

- No existe `infra/init.sql`: cada servicio crea su base al aplicar migraciones (`Database__AplicarMigraciones=true`). El asistente sigue ese mismo patrón (ver decisiones).
- No existe `docs/`; el diagrama está en `Documentaciones/arquitectura.drawio` (se usó ese).
- `CAMBIOS_CONTRATO.md` se eliminó en el commit `81f9620`. Se recrea en la raíz solo con las propuestas del asistente.

## 2. Contratos (`contracts/`)

- `evaluaciones.yaml`: solo `/mis-notas`, `/mis-notas/cursos/{cursoId}/actividades`, `/internal/actividades/{actividadId}` y health.
- `entregas.yaml`: `/actividades/{actividadId}/entregas` (POST y GET), `/entregas/{entregaId}` (PUT y DELETE), `/entregas/{entregaId}/archivos/{archivoId}`, `/mis-entregas` y health.
- No hay contrato de Usuarios ni del gateway.

## 3. Endpoints de lectura del estudiante (existen)

| Endpoint | Servicio | Respuesta |
|---|---|---|
| `GET /mis-notas` | Evaluaciones (`MisNotasController.cs:18`) | `MatrizNotasDto { cursos[{ cursoId, codigo, nombre, profesor, cortes[{corte, pesoCorte, nota?, publicado}], definitivaParcial, esParcial }] }` |
| `GET /mis-notas/cursos/{cursoId}/actividades` | Evaluaciones (`MisNotasController.cs:23`) | `{ cursoId, actividades[{ actividadId, titulo, corte, peso, fechaLimite?, estado: PUBLICADA \| SIN_CALIFICAR, nota?, retroalimentacion? }] }`. Un borrador sale como `SIN_CALIFICAR`. |
| `GET /mis-entregas[?actividadId=]` | Entregas (`EntregasController.cs:478`) | `[{ id, actividadId, estudianteId, fechaEnvio, estado, archivos[], tamanoTotal }]` |

Los tres usan el `sub` del token; ninguno recibe un `estudianteId`.

## 4. Endpoints del profesor en Evaluaciones (existen, fuera del contrato)

| Endpoint | Detalle |
|---|---|
| `GET /cursos` | `CursosController.cs:313`. Cursos del profesor según el token: `[{ id, codigo, nombre, profesor, pesoCorte1..3, actividadesPendientes }]` |
| `GET /cursos/{cursoId}/estudiantes` | `CursosController.cs:307`. Inscritos: `[{ id, nombre, codigo, rol }]`. Solo el profesor dueño. |
| `GET /cursos/{cursoId}/actividades` | `CursosController.cs:340`. `[{ id, cursoId, titulo, corte, peso, fechaLimite, requiereEntrega, vencida, estado }]` |
| `GET /actividades/{actividadId}/calificaciones` | `ActividadesController.cs:258`. `[{ id, actividadId, estudianteId, entregaId, valor, retroalimentacion, estado: BORRADOR \| PUBLICADA, version }]` |
| `PUT /actividades/{actividadId}/calificaciones/{estudianteId}` | `ActividadesController.cs:266`. Cuerpo `{ valor, retroalimentacion?, entregaId? }`. Siempre queda en **BORRADOR** (una nota publicada vuelve a borrador). Concurrencia: `If-Match` opcional con la `version` (ROWVERSION en base64); si no coincide, `409 CONFLICTO_CONCURRENCIA`. Responde la calificación con `ETag`. Errores: 403, 404, 422 `NOTA_FUERA_DE_RANGO`, 422 `ACTIVIDAD_SIN_ENTREGA`. |

**Conclusión: la fase B no está bloqueada.** Todos esos endpoints pasan por el gateway (`/cursos`, `/cursos/{**}`, `/actividades/{id}/calificaciones/{**}`).
Siguen faltando en `contracts/evaluaciones.yaml`; eso ya estaba pendiente en las secciones C y D del `CAMBIOS_CONTRATO.md` eliminado.

## 5. Gateway y Usuarios

- El gateway existe (`services/gateway/src/Gateway.Api`). Valida el JWT, aplica CORS, bloquea `/internal/**` (`Program.cs:87`) y enruta con YARP (`appsettings.json`). **No tenía ruta `/asistente/**`**: se agrega (cambio trivial permitido).
- Usuarios existe: `POST /auth/login` y `GET /auth/me`, y firma JWT RS256 (`sub`, `rol`, `nombre`).
- Por lo tanto `Services__GatewayBaseUrl` apunta al gateway real (`http://gateway:8080`). No hay desviación temporal.

## 6. Decisiones de `CAMBIOS_CONTRATO.md` (versión eliminada) ya aplicadas

- A (varios archivos por entrega, descarga): aplicada en código y en `entregas.yaml`.
- B (`fechaLimite` null, `estudianteId` opcional en `/internal`, `GET /actividades/{id}/entregas`): aplicada.
- C (PUT de calificación y publicar): aplicada en código; **no** está en `evaluaciones.yaml`.
- D (cursos, pesos, actividades): aplicada en código; **no** está en `evaluaciones.yaml`.
- Propuestas 1 a 6 (códigos extra, 200 frente a 201, etc.): no consta que se hayan decidido.

## 7. Primera comparación contra el diagrama

| Elemento | Estado |
|---|---|
| Frontend, Gateway, Usuarios, Evaluaciones (+ motor `MotorPonderado.cs`), Entregas, BD evaluaciones, BD entregas, Blob | Existen |
| BD usuarios | Existe en código, pero en Development (incluido docker-compose) corre en memoria |
| Entregas → Evaluaciones (REST, fecha límite) | Existe (`/internal/actividades/{id}` con `X-Service-Key`) |
| Servicio asistente IA, API asistente, BD asistente, ruta `/asistente/**`, Asistente → Gateway, Asistente → LLM | **Faltaban**: los crea este trabajo |

La tabla completa está en `CONFORMIDAD_DIAGRAMA.md`.

## 8. Plan de archivos

```
services/asistente/
  Dockerfile, README.md, Asistente.slnx
  src/Asistente.Domain/         AccionPendiente, RegistroAuditoria, ResolutorNombres, excepciones
  src/Asistente.Application/    ILlmClient, IGatewayCliente, repositorios, herramientas por rol,
                                CatalogoHerramientas, ProcesarMensaje, ConfirmarAccion, CancelarAccion
  src/Asistente.Infrastructure/ GroqLlmClient (HttpClient tipado), GatewayHttpCliente (reenvía JWT),
                                AsistenteDbContext + migración Inicial
  src/Asistente.Api/            Program, AsistenteController, traductor de excepciones, límite de mensajes
  tests/Asistente.UnitTests/         roles, resolución de nombres, acciones, arquitectura
  tests/Asistente.IntegrationTests/  WebApplicationFactory + Testcontainers SQL + WireMock como gateway y como Groq
contracts/asistente.yaml
CAMBIOS_CONTRATO.md                      (propuestas del asistente)
infra/docker-compose.yml, infra/.env.example
services/gateway/src/Gateway.Api/appsettings.json   (ruta /asistente/**)
UnillanosNotas.slnx                      (agrega los proyectos del asistente)
services/compartido/Unillanos.ServiceDefaults/Errores  (ErrorDescrito con estado opcional, compatible)
docs/asistente/CONFORMIDAD_DIAGRAMA.md
```
