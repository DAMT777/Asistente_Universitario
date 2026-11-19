# Servicio de horarios

El estudiante consulta las clases de sus cursos matriculados. El profesor consulta las clases de los cursos a su cargo. El servicio verifica el JWT y consulta Evaluaciones con la misma identidad para filtrar los bloques. La semana se muestra de lunes a sábado, de 06:00 a 18:00 en hora de Colombia.

En Docker Compose, el servicio escucha en el puerto 5005 y se inicia con el gateway. La aplicación consulta `GET /horarios/semana` por el gateway en el puerto 5000. El servicio es de consulta.

Los datos de demostración están en `src/Horarios.Api/appsettings.Development.json`. La configuración `Horarios:Bloques` contiene el ID del curso de Evaluaciones, el día ISO (lunes = 1, sábado = 6), las horas en formato `HH:mm` y el aula. En otros entornos, configura la sección con los horarios oficiales antes del arranque. Por ejemplo, las variables de entorno del primer bloque son:

```dotenv
Horarios__Bloques__0__CursoId=<ID del curso en Evaluaciones>
Horarios__Bloques__0__Dia=1
Horarios__Bloques__0__Inicio=08:00
Horarios__Bloques__0__Fin=10:00
Horarios__Bloques__0__Aula=Laboratorio 1
```
