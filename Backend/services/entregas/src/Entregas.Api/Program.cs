// MARCADOR DE POSICIÓN del servicio de entregas (frente D). No guarda archivos ni valida fechas.
// Solo responde listas vacías en las rutas que el frontend consulta junto con las calificaciones, para que
// las pantallas del profesor carguen mientras el servicio real no existe. Se reemplaza por completo.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapGet("/actividades/{actividadId:guid}/entregas", (Guid actividadId) => Results.Ok(Array.Empty<object>()));
app.MapGet("/mis-entregas", () => Results.Ok(Array.Empty<object>()));
app.MapHealthChecks("/health/live");

app.Run();
