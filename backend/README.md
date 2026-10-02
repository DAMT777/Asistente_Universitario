# Backend

Servicios en ASP.NET Core sobre .NET 10, detrás de un API Gateway. Separado del frontend (`../Frontend`).

| Carpeta | Puerto | Qué es |
|---------|--------|--------|
| `services/gateway` | 5000 | Punto único de entrada (YARP). Valida el JWT, aplica CORS, bloquea `/internal/**` y enruta. |
| `services/usuarios` | 5001 | **CU-01.** Login contra `usuarios_db` (EF Core, contraseñas con el hasher de Identity), `/auth/me` y emisión del JWT RS256. |
| `services/evaluaciones` | 5002 | **CU-02, CU-03, CU-05 a CU-12, CU-15 y CU-16**, y el endpoint interno `/internal/actividades/{id}`. Capas Domain, Application, Infrastructure y Api. |
| `services/entregas` | 5003 | **CU-04**, entregas propias y descarga del archivo. Valida el JWT y verifica con Evaluaciones que el profesor sea dueño del curso. Capas Domain, Application, Infrastructure y Api. Subir, reemplazar y anular (CU-13 y CU-14) siguen pendientes. |

## Cómo correrlo

Requisitos: .NET 10 SDK, Node.js y OpenSSL. No hace falta Docker ni SQL Server: en desarrollo, Usuarios, Evaluaciones y Entregas usan una base en memoria con los datos semilla de la guía (sección 9.8). Entregas guarda los archivos de demostración en disco, en `entregas/src/Entregas.Api/almacen-entregas` (ignorada por git).

Entregas llama a Evaluaciones (`Services:EvaluacionesBaseUrl`, por defecto `http://localhost:5002`) con el encabezado `X-Service-Key`. En desarrollo los dos servicios traen la misma clave en `appsettings.Development.json`; en cualquier otro entorno se define con la variable `ServiceKey`, igual en ambos. Arranca Evaluaciones antes de pedir entregas: si no responde, Entregas contesta 503 `SERVICIO_NO_DISPONIBLE`.

```bash
# 1. Una sola vez: genera las llaves del token (quedan en infra/keys, fuera de git)
bash infra/scripts/generar-llaves.sh        # en PowerShell: infra/scripts/generar-llaves.ps1
```

Cada servicio va en su propia terminal, desde `Backend/services`:

```bash
dotnet run --project usuarios/src/Usuarios.Api
dotnet run --project evaluaciones/src/Evaluaciones.Api
dotnet run --project entregas/src/Entregas.Api
dotnet run --project gateway/src/Gateway.Api
```

Y el frontend apuntando al gateway, desde `Frontend/Interfaz/code`:

```bash
VITE_API_URL=http://localhost:5000 npm run dev
```

En PowerShell: `$env:VITE_API_URL='http://localhost:5000'; npm.cmd run dev`.

Usuarios de prueba (contraseña `Demo1234!`, solo existen en desarrollo):

| Código | Rol | Nombre |
|--------|-----|--------|
| `P0001` | PROFESOR | Profesor Demo |
| `E0001` | ESTUDIANTE | Ana Demo (corte 1 publicado con 3.2) |
| `E0002` | ESTUDIANTE | Luis Demo (tiene un borrador) |
| `E0003` | ESTUDIANTE | Marta Demo (sin calificaciones) |

## Pruebas

```bash
dotnet test services/evaluaciones/Evaluaciones.slnx   # 124 unitarias + 43 de integración
dotnet test services/entregas/Entregas.slnx           # 12 unitarias
dotnet test services/usuarios/Usuarios.slnx           # 12 de integración
dotnet test services/gateway/Gateway.slnx             # 22 de integración
```

Las de integración levantan la API real en memoria y firman sus propios tokens, así que no necesitan que haya nada corriendo.

## Qué funciona con el frontend y qué no

| Pantalla | Estado |
|----------|--------|
| Login (profesor y estudiante, CU-01) | Funciona con los usuarios de `usuarios_db`. Si el token vence o deja de ser válido, el frontend borra la sesión y vuelve al acceso con un aviso |
| Pesos y avance: **pesos de los cortes** (CU-02) | Funciona. El servicio exige que sumen 100 (`PESOS_CORTE_INVALIDOS`) |
| Actividades: **crear y editar** con corte, peso, fecha límite y si requiere entrega (CU-03) | Funciona. El servicio rechaza un corte que pase de 100 (`PESOS_ACTIVIDAD_EXCEDIDOS`) |
| Calificar: **entregas de la actividad** con fecha, archivo y descarga, y filtro *Con entrega / Sin entrega* (CU-04) | Funciona con el servicio de entregas |
| Inicio del profesor, lista de cursos y detalle del curso | Funciona |
| Notas por corte: tabla, avance y **Corregir publicación** (CU-09, CU-11) | Funciona |
| Notas por corte: **Publicar corte** (CU-10) | El botón se habilita según una regla de la pantalla. Con la semilla ningún estudiante la cumple, así que el endpoint se prueba con `curl` y con las pruebas de integración |
| Calificar: **calificar una entrega** (CU-05), **nota de actividad sin entrega** (CU-06), **modificar una nota** (CU-07) y **Publicar notas de actividad** (CU-08) | Funciona |
| Inicio, Cursos, Notas y detalle de actividad del estudiante (CU-12, CU-15, CU-16) | Funciona. Solo muestra notas publicadas con su retroalimentación |
| Subir, reemplazar y anular entregas (CU-13, CU-14) | Sin implementar. El estudiante ve sus entregas, pero al subir recibe un error |

### Recorrido para probar CU-01 a CU-04

1. **CU-01.** En el acceso elige *Soy docente* y entra con `P0001` / `Demo1234!`. Con una clave errada responde "Usuario o contraseña incorrectos".
2. **CU-02.** Abre el curso, pestaña **Pesos y avance**. Pon 30, 30 y 30: el botón se bloquea. Pon 25, 35 y 40 y pulsa **Guardar pesos**; la cabecera del curso cambia.
3. **CU-03.** Pestaña **Actividades** → **Nueva actividad**: *Taller 3*, corte 3, 30 %, una fecha futura. Luego **Editar actividad** sobre cualquier actividad y cambia el título.
4. **CU-04.** Pestaña **Calificar** → *Proyecto 1*: Ana y Luis aparecen con fecha de entrega y **Descargar**; usa el filtro *Con entrega* o *Sin entrega*.

### Recorrido para probar CU-05 a CU-08

1. Entra como `P0001`, abre el curso y la pestaña **Calificar**.
2. **CU-05.** Elige *Proyecto 1*: Ana y Luis tienen entrega con enlace de descarga. Escribe la nota y la retroalimentación de Ana y pulsa **Guardar borrador**.
3. **CU-06.** Elige *Parcial 1* (no requiere archivo) y registra la nota de Marta.
4. **CU-07.** En *Parcial 1*, cambia la nota publicada de Ana (3.0) y pulsa **Modificar nota**: vuelve a borrador.
5. **CU-08.** Pulsa **Publicar notas de actividad (n)**.
6. Entra como `E0003` (Marta) o `E0001` (Ana) y abre el curso: la nota y la retroalimentación aparecen solo después del paso 5.
