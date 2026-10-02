# Backend

Servicios en ASP.NET Core sobre .NET 10, detrás de un API Gateway. Separado del frontend (`../Frontend`).

| Carpeta | Puerto | Qué es |
|---------|--------|--------|
| `services/gateway` | 5000 | Punto único de entrada (YARP). Valida el JWT, aplica CORS, bloquea `/internal/**` y enruta. |
| `services/usuarios` | 5001 | **Mínimo.** Login, `/auth/me` y emisión del JWT RS256. Lo reemplaza el frente A. |
| `services/evaluaciones` | 5002 | **CU-05 a CU-12, CU-15 y CU-16.** Capas Domain, Application, Infrastructure y Api. |
| `services/entregas` | 5003 | **Marcador de posición.** Expone en memoria cuatro entregas de demostración (Taller 1 y Proyecto 1 de Ana y Luis) y su descarga, para poder calificar una entrega (CU-05). No recibe archivos. Lo reemplaza el frente D. |

## Cómo correrlo

Requisitos: .NET 10 SDK, Node.js y OpenSSL. No hace falta Docker ni SQL Server: en desarrollo, Evaluaciones usa una base en memoria con los datos semilla de la guía (sección 9.8).

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
dotnet test services/evaluaciones/Evaluaciones.slnx   # 94 unitarias + 35 de integración
dotnet test services/usuarios/Usuarios.slnx           # 12 de integración
dotnet test services/gateway/Gateway.slnx             # 22 de integración
```

Las de integración levantan la API real en memoria y firman sus propios tokens, así que no necesitan que haya nada corriendo.

## Qué funciona con el frontend y qué no

| Pantalla | Estado |
|----------|--------|
| Login (profesor y estudiante) | Funciona con usuarios reales |
| Inicio del profesor, lista de cursos y detalle del curso | Funciona |
| Notas por corte: tabla, avance y **Corregir publicación** (CU-09, CU-11) | Funciona |
| Notas por corte: **Publicar corte** (CU-10) | El botón se habilita según una regla de la pantalla. Con la semilla ningún estudiante la cumple, así que el endpoint se prueba con `curl` y con las pruebas de integración |
| Calificar: **calificar una entrega** (CU-05), **nota de actividad sin entrega** (CU-06), **modificar una nota** (CU-07) y **Publicar notas de actividad** (CU-08) | Funciona. Las entregas que se ven son las de demostración del marcador de posición |
| Inicio, Cursos, Notas y detalle de actividad del estudiante (CU-12, CU-15, CU-16) | Funciona. Solo muestra notas publicadas con su retroalimentación |
| Crear actividades, pesos, subir/anular entregas | Sin implementar. Son de los frentes B y D |

### Recorrido para probar CU-05 a CU-08

1. Entra como `P0001`, abre el curso y la pestaña **Calificar**.
2. **CU-05.** Elige *Proyecto 1*: Ana y Luis tienen entrega con enlace de descarga. Escribe la nota y la retroalimentación de Ana y pulsa **Guardar borrador**.
3. **CU-06.** Elige *Parcial 1* (no requiere archivo) y registra la nota de Marta.
4. **CU-07.** En *Parcial 1*, cambia la nota publicada de Ana (3.0) y pulsa **Modificar nota**: vuelve a borrador.
5. **CU-08.** Pulsa **Publicar notas de actividad (n)**.
6. Entra como `E0003` (Marta) o `E0001` (Ana) y abre el curso: la nota y la retroalimentación aparecen solo después del paso 5.
