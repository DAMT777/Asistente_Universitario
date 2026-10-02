# Backend

Servicios en ASP.NET Core sobre .NET 10, detrás de un API Gateway. Separado del frontend (`../Frontend`).

| Carpeta | Puerto | Qué es |
|---------|--------|--------|
| `services/gateway` | 5000 | Punto único de entrada (YARP). Valida el JWT, aplica CORS, bloquea `/internal/**` y enruta. |
| `services/usuarios` | 5001 | **Mínimo.** Login, `/auth/me` y emisión del JWT RS256. Lo reemplaza el frente A. |
| `services/evaluaciones` | 5002 | **CU-09, CU-10, CU-11 y CU-12.** Capas Domain, Application, Infrastructure y Api. |
| `services/entregas` | 5003 | **Marcador de posición.** Solo responde listas vacías para que las pantallas del profesor carguen. Lo reemplaza el frente D. |

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
dotnet test services/evaluaciones/Evaluaciones.slnx   # 65 unitarias + 23 de integración
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
| Inicio, Cursos y Notas del estudiante | Cargan el login y `/cursos` (CU-12), pero fallan en `/mis-notas` y `/mis-notas/cursos/{id}/actividades` (CU-15 y CU-16, aún sin implementar). La pantalla muestra el error con "Reintentar" |
| Calificar, crear actividades, pesos, entregas | Sin implementar. Son de los frentes B y D |
