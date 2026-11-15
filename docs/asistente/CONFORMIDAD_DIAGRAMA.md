# Conformidad del código con el diagrama de arquitectura

Diagrama: `Documentaciones/arquitectura.drawio` (no existe `docs/arquitectura.drawio`). Revisión del 2026-11-14.

| Elemento del diagrama | Tipo | Dónde está en el código | Estado | Evidencia | Acción propuesta |
|---|---|---|---|---|---|
| Aplicaciones cliente / Frontend | caja | `Frontend/` | cumple | Carpeta del frontend; consume `VITE_API_URL=http://localhost:5000` (`infra/docker-compose.yml:6`) | — |
| Vista profesor (incluye "asistente IA para calificar") | caja | `Frontend/Interfaz/code/src/features/asistente/VentanaAsistente.tsx` (chat flotante abierto desde la barra superior) | cumple | Chat con tarjeta de propuesta, cuenta regresiva y botones Confirmar/Cancelar | — |
| Vista estudiante (incluye "consulta notas con el asistente") | caja | Misma ventana flotante | cumple | Solo consultas; sin tarjeta de acciones | — |
| Frontend → API Gateway (HTTPS + token) | flecha | `services/gateway` | difiere | El gateway valida JWT y CORS, pero en local corre por HTTP (`docker-compose.yml`, puerto 5000) | HTTPS lo da el despliegue (Azure); documentarlo |
| API Gateway | caja | `services/gateway/src/Gateway.Api/Program.cs` | cumple | YARP, JWT RS256, bloqueo de `/internal/**` (`Program.cs:87`) | — |
| Gateway → Servicio de usuarios | flecha | `Gateway.Api/appsettings.json` rutas `login`, `auth` | cumple | Clúster `usuarios` | — |
| Servicio de usuarios / API auth (login, token, rol) | caja | `services/usuarios/src/Usuarios.Api` | difiere | Existe y firma JWT, pero es un solo proyecto sin capas Api/Application/Domain/Infrastructure y no usa `Unillanos.ServiceDefaults` (`Program.cs:1-60`) | Separar en capas y adoptar ServiceDefaults |
| BD usuarios | caja | `UsuariosDbContext.cs` | difiere | `appsettings.Development.json` → `Database:UseInMemory: true`; docker-compose no le pasa `ConnectionStrings__Default` (`docker-compose.yml`, servicio `usuarios`) | Darle `usuarios_db` en SQL Server en compose |
| Gateway → Servicio de evaluaciones | flecha | `appsettings.json` rutas `evaluaciones-*` | cumple | `/cursos`, `/mis-notas`, `/actividades/{id}`, `/actividades/{id}/calificaciones/**` | — |
| Servicio de evaluaciones y cursos / API evaluaciones | caja | `services/evaluaciones/src/*` | difiere | Capas correctas, pero no usa `Unillanos.ServiceDefaults`: middleware de errores propio (`Evaluaciones.Api/Middleware/ManejadorErroresMiddleware.cs`) y JWT propio (`Program.cs:553-599`). Sus endpoints de profesor no están en `contracts/evaluaciones.yaml` | Adoptar ServiceDefaults; completar el contrato |
| Motor de ponderado y publicación de cortes | caja | `Evaluaciones.Domain/Ponderado/MotorPonderado.cs`, `PublicarCorte.cs` | cumple | — | — |
| BD evaluaciones | caja | `EvaluacionesDbContext`, migración `Inicial`, `evaluaciones_db` en compose | cumple | — | — |
| Gateway → Servicio de entregas | flecha | `appsettings.json` rutas `entregas*` | cumple | — | — |
| Servicio de entregas / API entregas (archivos, fecha límite) | caja | `services/entregas/src/*` | cumple | Capas + ServiceDefaults; valida fecha (`PlazoEntrega.cs`) | — |
| BD entregas (metadatos) | caja | `EntregasDbContext`, `entregas_db` | cumple | — | — |
| Blob Storage | caja | `BlobAlmacenArchivos.cs`, Azurite en compose | cumple | — | — |
| Entregas → Evaluaciones (consulta fecha límite, REST) | flecha | `Entregas.Infrastructure/Evaluaciones/EvaluacionesHttpClient.cs` → `GET /internal/actividades/{id}` con `X-Service-Key` | cumple | Única llamada directa entre servicios; `Services__EvaluacionesBaseUrl=http://evaluaciones:8080` | — |
| Servicio asistente IA | caja | `services/asistente/` (nuevo) | cumple | 4 capas, ServiceDefaults, puerto 5004, health checks | — |
| API asistente (herramientas según rol, confirmación antes de escribir) | caja | `CatalogoHerramientas.cs`, `ProponerCalificacion.cs`, `GestionarAcciones.cs` | cumple | Pruebas `SeleccionHerramientasTests`, `ProfesorTests` | — |
| BD asistente (acciones pendientes, auditoría) | caja | `AsistenteDbContext.cs`, migración `Inicial`, `asistente_db` en compose | cumple | Tablas `AccionPendiente` y `Auditoria` | — |
| Nota "Sin acceso a otras BD ni X-Service-Key" | restricción | `ArquitecturaTests.cs` | cumple | La prueba falla si aparece una referencia a otro servicio, `ServiceKey` o `/internal`; los dobles HTTP verifican que no se envía | — |
| Gateway → Servicio asistente IA (`/asistente/**`) | flecha | `Gateway.Api/appsettings.json` ruta `asistente`, compose `ReverseProxy__Clusters__asistente…` | cumple | Agregada en este trabajo | — |
| Asistente → Gateway (con el JWT del usuario) | flecha | `GatewayHttpCliente.cs`, `ReenviarJwtHandler.cs`, `Services__GatewayBaseUrl=http://gateway:8080` | cumple | Prueba `Nunca_envia_llave_de_servicio_ni_llama_rutas_internas` | — |
| Asistente → Proveedor LLM (HTTPS) | flecha | `GroqLlmClient.cs`, `Llm__BaseUrl=https://api.groq.com/openai/v1` | cumple | Pruebas `ProveedorLlmTests` | — |
| Proveedor LLM (externo, el diagrama dice LLaMA) | caja | Groq, `LLM_MODEL=openai/gpt-oss-120b` | difiere | Groq retiró `llama-3.3-70b-versatile` (404 en la prueba real del 2026-11-15); sus únicos Llama vigentes son `llama-prompt-guard`, que no conversan ni usan herramientas | Actualizar el diagrama a "Proveedor LLM (Groq)" o volver a un Llama cuando Groq ofrezca uno con herramientas; basta cambiar `LLM_MODEL` |
| Azure (entorno de despliegue) | caja | `infra/docker-compose.yml` (solo local) | falta | No hay plantillas de despliegue a Azure (Bicep, ARM, Terraform o pipeline) | Pendiente del equipo |
| (sin flecha) Asistente → Evaluaciones o Entregas directo | dependencia prohibida | — | cumple | No existe; verificado por `ArquitecturaTests` | — |
| (sin flecha) Servicio que lee la BD de otro | dependencia prohibida | — | cumple | Cada servicio usa su propia `ConnectionStrings__Default` | — |

## Resumen

- 28 elementos (incluidas las 2 dependencias prohibidas verificadas): **22 cumplen, 5 difieren, 1 falta**.
- Los tres incumplimientos más importantes:
  1. **BD usuarios** corre en memoria también en docker-compose: la caja "BD usuarios" no tiene una base real.
  2. **Evaluaciones y Usuarios no usan `Unillanos.ServiceDefaults`**: tienen su propio middleware de errores y su propio JWT. Rompe el "único middleware" de errores, y en Evaluaciones los errores salen con `traceId = Activity.Id` en lugar de `X-Correlation-Id`.
  3. **No hay definición de despliegue en Azure**: la caja que contiene todo el backend no tiene contraparte en el repositorio.
- No se corrigió nada fuera de `services/asistente/`, de `infra/` y de la ruta del gateway.
