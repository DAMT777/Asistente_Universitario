# Servicio asistente IA (puerto 5004)

Asistente en lenguaje natural sobre el sistema de notas. Opcional: el resto del sistema funciona sin él.

- **Estudiante:** consulta sus notas publicadas, la matriz de cortes y sus entregas (solo lectura).
- **Profesor:** pide una calificación ("ponle 4.2 a Luis en el Taller 1"). El asistente crea una **acción pendiente**
  (vence en 5 minutos) y solo escribe cuando el profesor llama a `POST /asistente/acciones/{id}/confirmar`. Siempre queda en borrador.

Reglas de seguridad: el asistente no tiene poderes propios. Reenvía el JWT del usuario y llama a Evaluaciones y Entregas
**a través del gateway** (`Services__GatewayBaseUrl`). Nunca usa `/internal/**`, `X-Service-Key` ni la base de otro servicio.
Las herramientas que recibe el modelo dependen del claim `rol`. Contrato: [`contracts/asistente.yaml`](../../contracts/asistente.yaml).

## Configuración (variables de entorno)

| Variable | Defecto | |
|---|---|---|
| `ConnectionStrings__Default` | — | `asistente_db` |
| `Services__GatewayBaseUrl` | — | p. ej. `http://gateway:8080` |
| `Llm__BaseUrl` | `https://api.groq.com/openai/v1` | API compatible con OpenAI |
| `Llm__ApiKey` | — | Solo en `infra/.env` (`LLM_API_KEY`) |
| `Llm__Model` | — | p. ej. `openai/gpt-oss-20b` |
| `Llm__TimeoutSeconds` | 15 | |
| `Asistente__ExpiracionAccionMinutos` | 5 | |
| `Asistente__MaxLlamadasHerramientas` | 5 | por mensaje |
| `Asistente__MensajesPorMinuto` | 10 | por usuario; 429 `LIMITE_MENSAJES` |
| `Jwt__PublicKeyPath`, `Jwt__Issuer`, `Jwt__Audience` | | igual que los demás servicios |

## Correr

```bash
cp infra/.env.example infra/.env   # y poner LLM_API_KEY
docker compose -f infra/docker-compose.yml up --build
```

## Ejemplos (a través del gateway, puerto 5000)

```bash
TOKEN=$(infra/scripts/generar-token.sh ana)
curl -s -X POST http://localhost:5000/asistente/mensajes \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"mensaje":"¿Cuánto saqué en el Taller 1?"}'
```

```bash
TOKEN=$(infra/scripts/generar-token.sh profesor)
curl -s -X POST http://localhost:5000/asistente/mensajes \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"mensaje":"Ponle 4.2 a Luis en el Taller 1, buen trabajo"}'
# La respuesta trae accionPendiente.id. Para guardar la nota (en borrador):
curl -s -X POST http://localhost:5000/asistente/acciones/<id>/confirmar -H "Authorization: Bearer $TOKEN"
# o para descartarla:
curl -s -X POST http://localhost:5000/asistente/acciones/<id>/cancelar -H "Authorization: Bearer $TOKEN"
```

## Pruebas

```bash
dotnet test services/asistente/Asistente.slnx
```

Ninguna prueba llama a Groq: usan un doble de `ILlmClient` con respuestas guionizadas y WireMock como gateway y como proveedor.
Las de integración necesitan Docker (SQL Server con Testcontainers).

## Importar notas como profesor

En Sirius, pulsa el clip **Adjuntar Excel de notas**, selecciona la materia y la actividad y carga un archivo .xlsx. La primera hoja debe contener dos columnas, en este orden:

| codigo | nota |
|---|---|
| E0001 | 3.8 |
| E0002 | 4.2 |

Usa el código institucional exacto, en formato texto si tiene ceros al inicio. El archivo admite hasta 250 estudiantes y 256 KB. Las notas van de 0 a 5 con máximo un decimal. La vista previa muestra las notas actuales y las nuevas. Al confirmar, las notas nuevas o modificadas quedan en borrador. Si una calificación cambió después de la vista previa, revisa el archivo nuevamente.

El navegador lee el Excel y envía a Evaluaciones únicamente los identificadores, notas y versiones necesarios para guardar el lote. El modelo de IA no recibe el archivo ni participa en la importación.

## Consultar el horario en Sirius

El profesor y el estudiante pueden preguntar «¿Cuál es mi horario inscrito?» o «¿En qué aula tengo clase el jueves?». Sirius consulta `GET /horarios/semana` con el JWT del usuario. Los datos se configuran en el [servicio de horarios](../horarios/README.md). Para incorporar otra consulta, agrega una herramienta que lea el endpoint autorizado del servicio correspondiente y declárala para los roles permitidos.

Sirius permite consultar actividades, cortes, fechas límite y requisitos de entrega de los cursos propios. El docente también puede consultar el estado de las notas de una actividad y la nota de un estudiante inscrito. El chat rechaza patrones de sintaxis SQL antes de llamar al proveedor. Las herramientas solo llaman rutas públicas predefinidas y autorizadas por el JWT.
