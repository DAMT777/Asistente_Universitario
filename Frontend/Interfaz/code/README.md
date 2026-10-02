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

### Con el backend real

La parte del estudiante (CU-13 a CU-16) ya habla con los servicios de Evaluaciones y Entregas:

```bash
# 1. Backend (desde la raíz del repositorio)
cp infra/.env.example infra/.env          # edite los valores
infra/scripts/generar-llaves.sh
docker compose -f infra/docker-compose.yml up --build

# 2. Frontend (desde esta carpeta)
npm run dev:backend                       # http://localhost:5173
```

En modo `backend`, el servidor de Vite hace de gateway local, así que no hay problemas de CORS:

| Ruta en el navegador | Va a |
| --- | --- |
| `/api/mis-notas/**` | Evaluaciones (`EVALUACIONES_URL`, por defecto `http://localhost:5002`) |
| `/api/mis-entregas`, `/api/entregas/**`, `/api/actividades/{id}/entregas` | Entregas (`ENTREGAS_URL`, por defecto `http://localhost:5003`) |
| `/api/auth/login`, `/api/auth/logout` | Doble de Usuarios de desarrollo (`dev/autenticacionDev.ts`) |
| Cualquier otra `/api/**` | 501 `NO_IMPLEMENTADO` (funciones del docente aún sin backend) |

El doble de Usuarios solo existe en `npm run dev:backend`: firma JWT RS256 con `infra/keys/jwt-privada.pem` (la llave nunca llega al navegador) para los usuarios semilla, todos con la contraseña `Demo1234!`:

- Estudiantes: `E0001` (Ana: tiene notas publicadas), `E0002` (Luis: solo un borrador), `E0003` (Marta: sin notas), `E0099` (Pedro: no inscrito).
- Docente: `P0001`. Inicia sesión, pero sus pantallas todavía no tienen backend.

Cuando exista el API Gateway, todo `/api` se envía allí y el login lo atiende Usuarios:

```bash
GATEWAY_URL=http://localhost:5000 npm run dev:backend
```

Para un build que apunte a un gateway publicado: `VITE_API_URL=https://gateway.ejemplo.edu.co npm run build`.

## Estructura

```
src/
  types/      Modelos y tipos del dominio
  schemas/    Validación con Zod (formularios y respuestas del backend)
  domain/     Motor de ponderado, estados de calificación, reglas y fechas
  api/        Contrato ApiClient, cliente HTTP (fetch) y api simulada
dev/          Solo para `vite`: doble de Usuarios (login de desarrollo)
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
| `useMisNotas()` | Estudiante | Matriz del backend (cortes publicados y definitiva parcial), actividades con su nota y su entrega |
| `useNotasCurso(id)` | Estudiante | Lo mismo para un curso |
| `useProximasEntregas(n)` | Estudiante | Próximas actividades ordenadas por fecha |
| `useEntrega(actividadId)` | Estudiante | Estado, `abierta`, `entregar(archivos)` (sube o reemplaza), `anular()` y `descargar(archivoId)` |
| `useCalificaciones(cursoId, actividadId)` | Docente | Filas por estudiante, conteos, `guardar()` (borrador o publicar) y `publicarBorradores()` |
| `usePonderado(cursoId)` | Docente | Borrador de pesos, validación en vivo (cada grupo suma 100) y acumulado por estudiante |
| `usePendientesDocente()` | Docente | Actividades con entregas sin calificar o en borrador |
| `useCrearActividad(cursoId)` | Docente | `crear()` con validación Zod |
| `useSesion()` | Ambos | `login()`, `logout()` y sesión actual |

## Reglas de negocio

- Notas de 0.0 a 5.0 con un decimal; se aceptan `3.5` y `3,5`.
- Cada curso define los pesos de sus tres cortes, que suman 100. Las actividades de cada corte también suman 100.
- Para el estudiante, la nota de cada corte y la definitiva parcial las calcula el backend sobre los cortes publicados; el frontend no las recalcula. En la api simulada se calculan con `domain/ponderado.ts`.
- El estudiante no ve los borradores: para él, una actividad en borrador aparece como "Entregada" o "Pendiente".
- Las entregas se cierran en la fecha límite o cuando la actividad ya está calificada. Mientras siga abierta, la entrega se puede reemplazar o anular.
- Una entrega tiene uno o varios archivos: se eligen, se revisan en la lista (se pueden quitar o agregar más) y solo se envían al pulsar **Entregar**. Se admiten PDF, Word, Excel, PowerPoint, ZIP, PNG o JPG; hasta 10 archivos, 20 MB por archivo y 50 MB en total. El backend además revisa que el contenido corresponda a la extensión.
- Al hacer clic en un archivo entregado se descarga con su nombre original, sea del tipo que sea.
- Si el docente edita una nota publicada, vuelve a borrador hasta que la publique de nuevo.
- La nota aprobatoria (3.0) se inyecta en `ApiProvider`.

## Diseño

Mobile first: tarjetas y listas en pantallas pequeñas y tabla en Mis notas a partir de 860 px. Las pantallas tienen fondo oscuro con la foto del campus desenfocada y paneles translúcidos. El rojo institucional marca la navegación activa y las acciones principales; cada curso tiene un color propio (rojo, violeta o verde) que se usa en su monograma y en sus barras. La tipografía es Public Sans. Todos los valores salen de `theme/tokens.ts`.
