# Propuestas de cambio al contrato

El archivo anterior se eliminó en el commit `81f9620`. Este solo recoge las propuestas del servicio asistente IA.

## Asistente IA (`contracts/asistente.yaml`, nuevo)

### 1. Códigos de error nuevos

| Código | Estado | Cuándo |
|---|---|---|
| `ACCION_NO_ENCONTRADA` | 404 | La acción no existe o es de otro usuario (no se revela cuál de los dos). |
| `ACCION_EXPIRADA` | 410 | Pasaron más de `Asistente__ExpiracionAccionMinutos` (5 por defecto) desde la propuesta. |
| `ACCION_YA_USADA` | 409 | La acción ya fue confirmada, cancelada o falló. **No estaba en la lista pedida**: se agregó porque "confirmar dos veces da error" necesita un código y ninguno existente lo describe. |
| `LIMITE_MENSAJES` | 429 | Más de `Asistente__MensajesPorMinuto` mensajes por usuario en un minuto. |
| `ASISTENTE_NO_DISPONIBLE` | 503 | El proveedor de IA responde 429, 5xx o se agota `Llm__TimeoutSeconds`. |

Propuesta: agregarlos a la lista oficial de códigos.

### 2. La confirmación reenvía los errores de Evaluaciones

`POST /asistente/acciones/{id}/confirmar` responde con el mismo estado y código que dio Evaluaciones (`CONFLICTO_CONCURRENCIA` 409, `SIN_PERMISO` 403, `NOTA_FUERA_DE_RANGO` 422...). Una acción cuya escritura falló queda `FALLIDA` y no se puede reintentar: hay que proponer de nuevo.

### 3. Endpoints de Evaluaciones que el asistente usa y no están en `evaluaciones.yaml`

`GET /cursos`, `GET /cursos/{cursoId}/estudiantes`, `GET /cursos/{cursoId}/actividades`, `GET /actividades/{actividadId}/calificaciones` y `PUT /actividades/{actividadId}/calificaciones/{estudianteId}` (con `If-Match`).
Ya existen en el código y pasan por el gateway. Propuesta: documentarlos en `contracts/evaluaciones.yaml` (estaba pendiente desde las integraciones C y D).

### 4. Cambio compatible en `Unillanos.ServiceDefaults`

`ErrorDescrito` tiene un tercer parámetro opcional `Status` y `ManejadorExcepciones` lo usa. Sirve para responder códigos que no están en la tabla común (los nuevos del asistente y los reenviados de Evaluaciones). Los servicios existentes no cambian de comportamiento.
