# Aula Unillanos

Plataforma para consultar actividades, enviar entregas y llevar notas por cortes. Tiene dos roles: estudiante y docente.
Versión web en React, Vite y TypeScript, pensada como base de una futura app en React Native.

## Cómo correrlo

```bash
npm install
npm run dev        # http://localhost:5173
npm test           # pruebas del motor de ponderado y validaciones
npm run lint       # incluye la verificación de límites entre capas
```

Sin `VITE_API_URL`, la app usa la api simulada (`src/api/mock`). Esa api trabaja con datos en memoria y fija la hora en el 1 de octubre de 2026, a las 12:00 en Colombia. Los cambios de la demostración se conservan al cambiar de rol y se reinician al recargar.
Usuarios de prueba (cualquier contraseña de 6 o más caracteres):

- Estudiante: `160005017`
- Docente: `lrincon`

Para conectar un backend real:

```bash
VITE_API_URL=https://api.ejemplo.edu.co npm run dev
```

## Estructura

```
src/
  types/      Modelos y tipos del dominio
  schemas/    Validación con Zod (formularios y respuestas del backend)
  domain/     Motor de ponderado, estados de calificación, reglas y fechas
  api/        Contrato ApiClient, cliente HTTP (fetch) y api simulada
  hooks/      useMisNotas, useCalificaciones, usePonderado, useEntrega, useSesion…
  theme/      tokens.ts: colores, espaciados, tipografía, radios y movimiento
  ui/         Componentes visuales web (Panel, Chip, Barra, Pestanas, Campo…)
  features/   Pantallas: auth, layout, estudiante, docente
  app/        main, router y providers (dependen de la plataforma)
```

Las dependencias van en una sola dirección:

```
types ← schemas ← domain ← api ← hooks ← ui / features ← app
                    theme ←────────────── ui / features
```

`.eslintrc.cjs` hace cumplir estos límites:

- `types/`, `schemas/`, `domain/` y `theme/` no pueden importar React, React DOM, el router ni nada de `ui/`, `features/`, `api/` o `hooks/`.
- `api/` no puede importar React ni la UI. Usa solo `fetch` y `FormData`, que también existen en React Native.
- `hooks/` puede usar React y TanStack Query, pero no React DOM, el router ni la UI web.

## Qué se reutiliza al pasar a React Native

**Se reutiliza tal cual:**

- `src/types/`: TypeScript puro.
- `src/schemas/`: Zod funciona igual en React Native.
- `src/domain/`: cálculo de cortes y acumulado, la nota necesaria para aprobar, los estados (pendiente, vencida, entregada, sin calificar, borrador, publicada, calificada) y las reglas de pesos y entregas. Tiene pruebas en `ponderado.test.ts`.
- `src/api/`: contrato, cliente HTTP y mock. En móvil cambia solo el archivo de entrega: `ArchivoEntrega.datos` pasa a ser `{ uri, name, type }` y `FormData` lo acepta así.
- `src/hooks/`: el cliente móvil monta los mismos `ApiProvider`, `SesionProvider` y `QueryClientProvider`. Solo cambia el `AlmacenSesion`, que pasa a AsyncStorage o SecureStore.
- `src/theme/tokens.ts`: valores planos, listos para `StyleSheet.create`. Lo exclusivo de la web (blur y sombras CSS) está separado en `web`.

**Se reescribe:**

- `src/ui/`: usa `div`, `button`, `input` y CSS. En móvil se reemplaza por `View`, `Pressable`, `TextInput`, `expo-blur`, etc., con los mismos nombres y props (`Panel`, `Chip`, `Monograma`, `Barra`, `Pestanas`, `Campo`) para que las pantallas cambien poco.
- `src/ui/useIsWide.ts`: pasa a `useWindowDimensions()`.
- `src/features/`: las pantallas se reescriben con los componentes nativos, pero conservan las mismas llamadas a hooks. La lógica no se toca.
- `src/app/`: el router pasa a React Navigation o Expo Router; `providers.tsx` se adapta al almacén nativo.
- `global.css` e `index.html`: no aplican en móvil.

## Hooks principales

| Hook | Rol | Devuelve |
| --- | --- | --- |
| `useMisNotas()` | Estudiante | Resumen ponderado por curso, actividades con estado, total de créditos |
| `useNotasCurso(id)` | Estudiante | Lo mismo para un curso |
| `useProximasEntregas(n)` | Estudiante | Próximas actividades ordenadas por fecha |
| `useEntrega(actividadId)` | Estudiante | Estado, `abierta` y `entregar(archivo)` con validación de formato y tamaño |
| `useCalificaciones(cursoId, actividadId)` | Docente | Filas por estudiante, conteos, `guardar()` (borrador o publicar) y `publicarBorradores()` |
| `usePonderado(cursoId)` | Docente | Borrador de pesos, validación en vivo de pesos y errores con reintento y acumulado por estudiante |
| `usePendientesDocente()` | Docente | Actividades con entregas sin calificar o en borrador |
| `useCrearActividad(cursoId)` | Docente | `crear()` con validación Zod |
| `useSesion()` | Ambos | `login()`, `logout()` y sesión actual |

## Reglas de negocio

- Notas de 0.0 a 5.0 con un decimal; se aceptan `3.5` y `3,5`.
- Cada curso define los pesos de sus tres cortes, que suman 100. Las actividades de cada corte pueden sumar menos de 100, pero nunca superarlo.
- La nota de avance de un corte es la suma de los aportes publicados, sin normalizar. Publicar una actividad no publica el corte. El profesor publica o corrige el corte por estudiante; la matriz suma únicamente esas publicaciones y marca la definitiva como parcial mientras falten cortes.
- El estudiante no ve notas ni comentarios en borrador. Las entregas se muestran independientemente de la publicación de una nota.
- Las entregas, sus reemplazos y sus anulaciones se permiten hasta la hora límite inclusive, aunque exista calificación. Anular conserva la nota. Se admiten PDF, DOCX, XLSX, PPTX, ZIP, PNG y JPG de hasta 20 MB, siguiendo la propuesta DA-04 de la guía.
- Si el docente edita una nota publicada, vuelve a borrador hasta que la publique de nuevo.
- La nota aprobatoria (3.0) se inyecta en `ApiProvider`.

## Diseño

Mobile first: tarjetas y listas en pantallas pequeñas y tabla en Mis notas a partir de 860 px. La interfaz sigue el Manual de Identidad Visual de Unillanos (MN-GCOM-001): rojo institucional `#E3061D` (Pantone 485C · RGB 227,6,29), blanco y grises tenues, con degradados claros y sin fondos oscuros. El logo oficial (`public/logo-unillanos.png`) aparece en el acceso y en la cabecera. El acceso por rol se abre en un modal blanco sobre un overlay blanco translúcido con desenfoque (`Modal` en `ui/`). El rojo institucional marca la navegación activa y las acciones principales; cada curso tiene un color propio (rojo, violeta o verde) que se usa en su monograma y en sus barras. La tipografía es Public Sans. Todos los valores salen de `theme/tokens.ts`.

Microinteracciones (solo web, en `ui/global.css` con una sola curva `motion.easing`): onda (ripple) en botones principales (`ui/onda.ts`, clase `au-onda-host`), llenado progresivo de las barras (`Barra`, `BarraCortes`), elevación con acento rojo en tarjetas y filas de cursos, y un menú flotante de accesibilidad (`ui/Accesibilidad.tsx`: tamaño de texto, alto contraste, subrayado de enlaces, espaciado de lectura y reducción de animaciones, guardado en `localStorage`). Todo respeta `prefers-reduced-motion`. En React Native, la onda pasa a `android_ripple`/`Pressable` y las animaciones a `Animated` o Reanimated.

## Flujos actualizados

- Docente: Actividades permite crear y editar, incluyendo actividades sin entrega y sin fecha obligatoria.
- Calificar ofrece tabla en escritorio y tarjetas en móvil, búsqueda por nombre/código, filtros y guardado con estado visible. Guardar deja borradores; Publicar notas de actividad publica los borradores de la actividad completa.
- Notas por corte muestra avance acumulativo, publicación vigente y notas en borrador por estudiante. La omisión de borradores exige una elección expresa. Corregir recalcula solo el estudiante seleccionado.
- Estudiante: la matriz usa tablas semánticas en escritorio y tarjetas en móvil. Sin publicar no se sustituye por cero. Las entregas muestran fecha/hora de Colombia, tamaño, descarga y anulación con confirmación.
- La fotografía del campus y los colores institucionales se conservan. El login agrupa campos y botón; la demostración identifica el uso de datos semilla.

## Integración con los servicios

Con `VITE_API_URL` apuntando al gateway, Calificar usa el backend real: **Guardar borrador / Actualizar borrador / Modificar nota** llaman a `PUT /actividades/{id}/calificaciones/{estudianteId}` con `{ valor, retroalimentacion, entregaId? }` (se envía `entregaId` cuando el estudiante tiene una entrega, CU-05) y **Publicar notas de actividad** llama a `POST /actividades/{id}/calificaciones/publicar` y muestra cuántas se publicaron (CU-08). Al modificar una nota ya publicada, la fila avisa que vuelve a borrador y que el corte, si estaba publicado, se corrige en Notas por corte (CU-07).


El repositorio contiene el frontend, no los microservicios ni sus archivos OpenAPI. La implementación funcional y los recorridos verificados utilizan la API simulada. El adaptador HTTP traduce los DTO externos de los ejemplos de la guía a los modelos de pantalla: roles PROFESOR/ESTUDIANTE, accessToken, fechaLimite, valor y retroalimentacion. Consulta calificaciones y entregas por separado y utiliza las rutas documentadas para publicar, corregir, reemplazar, anular y descargar. Las descargas requieren el token y se solicitan bajo demanda.

Antes de activar VITE_API_URL, validar los DTO de listado con el equipo. La guía no especifica el JSON de GET /cursos/{id}/ponderado; el adaptador propone que incluya `publicaciones: [{ estudianteId, corte, nota, fechaPublicacion? }]` para comparar avance y publicación vigente. Esta forma es una propuesta de integración, no un contrato aprobado. El listado de notas de actividad del estudiante se consume como una lista de `{ actividadId, valor, retroalimentacion, estado? }`; las listas de cursos y estudiantes admiten los DTO normalizados usados aquí y los campos de la guía.

Actualizar pesos reales usa llamadas separadas al curso y a las actividades. Se aplican primero las disminuciones de peso para no superar 100 durante el cambio. Si una llamada falla, se recargan los datos; la operación no es transaccional entre endpoints. El backend debe calcular y validar de nuevo todas las reglas, controlar permisos y concurrencia. Las pruebas del adaptador usan respuestas simuladas y no sustituyen una prueba con el gateway real.

## Verificación

`npm run build`, `npm run lint` y `npm test`. Las pruebas cubren corte incompleto sin normalización, cero frente a ausencia, publicación independiente, borradores, corrección individual, privacidad de notas, entregas tras calificar, anulación, límites de archivo, vencimiento inclusive y adaptación de mensajes HTTP.
