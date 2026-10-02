# Propuestas de cambio al contrato

No se cambió en silencio ningún contrato. `contracts/entregas.yaml` y `contracts/evaluaciones.yaml` reflejan lo acordado.
Estos son los vacíos que aparecieron al implementar CU-13 a CU-16 y cómo se resolvieron mientras el equipo decide.

## 1. Códigos de error para casos que no están en la lista

La lista acordada no cubre todas las respuestas de error que puede dar el framework. Hoy se responden con el formato estándar y estos códigos:

| Situación | Estado | Código usado hoy | Propuesta |
|---|---|---|---|
| Excepción no controlada | 500 | `ERROR_INTERNO` | Agregarlo a la lista oficial. |
| Método HTTP no soportado en una ruta existente | 405 | `METODO_NO_PERMITIDO` | Agregarlo. |
| La subida no viene como `multipart/form-data` | 415 | `TIPO_CONTENIDO_NO_SOPORTADO` | Agregarlo para no confundirlo con `TIPO_ARCHIVO_NO_PERMITIDO`, que se refiere al archivo. |
| Dos primeras subidas simultáneas de la misma actividad y estudiante (choca la restricción única) | 500 | `ERROR_INTERNO` | Definir 409 `ENTREGA_EN_CONFLICTO` y que el cliente reintente. |

## 2. POST de una entrega que ya existía

RN-07 / DA-05 indican que volver a subir reutiliza la misma fila. El contrato solo define 201 para el POST, y eso se devuelve aunque se reemplace una entrega existente o anulada.
**Propuesta:** 201 cuando se crea la fila y 200 cuando se reutiliza, para que el cliente lo distinga.

## 3. PUT sobre una entrega ANULADA

No está definido. Hoy se acepta (si la fecha está vigente), reemplaza el archivo y deja la entrega `ENVIADA`, igual que volver a subir (DA-05).
**Propuesta:** confirmarlo o responder 422 con un código nuevo (`ENTREGA_ANULADA`).

## 4. Autenticación de `/internal/**`

Falta o es inválida `X-Service-Key` → 401 `NO_AUTENTICADO`.
**Propuesta:** dejarlo documentado así. Ya está en `evaluaciones.yaml`.

## 5. Datos del curso que el frontend muestra y el backend no expone

El frontend ya consume los contratos del estudiante tal como están (`src/api/http.ts`, sección `estudiante`). Le faltan datos que hoy oculta cuando no llegan:

| Campo | Dónde se usa | Propuesta |
|---|---|---|
| `creditos` | Mis notas (columna y total de créditos), tarjetas de curso | Agregar a `CursoMatriz` en `GET /mis-notas`. |
| `periodo` (p. ej. `2026B`) | Inicio ("Semestre …") y código del curso | Agregarlo a `CursoMatriz`. |
| `grupo` | Código del curso (`603803-1`) | Agregarlo a `CursoMatriz`. |

## 6. Login (servicio de Usuarios)

No hay contrato de Usuarios todavía. El frontend llama `POST /auth/login` con `{ usuario, contrasena, rol: 'estudiante'|'docente' }` y espera `{ token, usuario: { id, nombre, codigo, rol: 'estudiante'|'docente' } }`, más `POST /auth/logout` (204).
El doble de desarrollo (`Frontend/Interfaz/code/dev/autenticacionDev.ts`) responde exactamente eso y firma el JWT con los claims acordados (`sub`, `rol` en mayúsculas, `nombre`, `iss`, `aud`, `iat`, `exp`).
**Propuesta:** que `contracts/usuarios.yaml` use esta forma, o avisar para ajustar `src/api/http.ts`.

## 7. Escala de los decimales

`peso` y `pesoCorte` salen tal como están en la base `DECIMAL(5,2)`, por ejemplo `30.00`. Las notas calculadas salen con 1 decimal (`1.0`).
**Propuesta:** aclarar en el contrato que son números y que el cliente no debe depender de la escala.
