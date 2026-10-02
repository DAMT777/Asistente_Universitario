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

Sin `VITE_API_URL`, la app usa la api simulada (`src/api/mock`). Esa api trabaja con datos en memoria y fija la fecha de hoy en el 1 de octubre de 2026.
Usuarios de prueba (cualquier contraseña de 6 o más caracteres):

- Estudiante: `160005017`
- Docente: `lrincon`

Para conectar un backend real:

```bash
VITE_API_URL=https://api.ejemplo.edu.co npm run dev
```

## Backend

El servicio de evaluaciones (.NET 10) está en `services/evaluaciones/` y el contrato en `contracts/`. Con él, `src/api/http.ts` ya cubre calificar, modificar y publicar notas (CU-05 a CU-08). Ver `services/evaluaciones/README.md`.

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
| `usePonderado(cursoId)` | Docente | Borrador de pesos, validación en vivo (cada grupo suma 100) y acumulado por estudiante |
| `usePendientesDocente()` | Docente | Actividades con entregas sin calificar o en borrador |
| `useCrearActividad(cursoId)` | Docente | `crear()` con validación Zod |
| `useSesion()` | Ambos | `login()`, `logout()` y sesión actual |

## Reglas de negocio

- Notas de 0.0 a 5.0 con un decimal; se aceptan `3.5` y `3,5`.
- Cada curso define los pesos de sus tres cortes, que suman 100. Las actividades de cada corte también suman 100.
- La nota de un corte es el promedio ponderado de las notas publicadas. El acumulado es la suma del aporte de cada corte.
- El estudiante no ve los borradores: para él, una actividad en borrador aparece como "Entregada".
- Las entregas se cierran en la fecha límite o cuando la actividad ya está calificada. Se admiten PDF, DOCX o ZIP de hasta 10 MB.
- Si el docente edita una nota publicada, vuelve a borrador hasta que la publique de nuevo.
- La nota aprobatoria (3.0) se inyecta en `ApiProvider`.

## Diseño

Mobile first: tarjetas y listas en pantallas pequeñas y tabla en Mis notas a partir de 860 px. Las pantallas tienen fondo oscuro con la foto del campus desenfocada y paneles translúcidos. El rojo institucional marca la navegación activa y las acciones principales; cada curso tiene un color propio (rojo, violeta o verde) que se usa en su monograma y en sus barras. La tipografía es Public Sans. Todos los valores salen de `theme/tokens.ts`.
