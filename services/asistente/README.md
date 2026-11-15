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
| `Llm__Model` | — | p. ej. `openai/gpt-oss-120b` |
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
