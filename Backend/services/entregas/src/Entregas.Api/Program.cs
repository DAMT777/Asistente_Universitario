// MARCADOR DE POSICIÓN del servicio de entregas (frente D). No recibe archivos ni valida fechas.
// Solo expone, en memoria, unas entregas de demostración para que el profesor pueda calificar una entrega (CU-05)
// desde el frontend mientras el servicio real no existe. Se reemplaza por completo.
//
// No valida el token: confía en que el gateway ya lo validó, y solo lee el claim "sub" para /mis-entregas.
// El servicio real debe validar la firma igual que Evaluaciones (sección 12.1).
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
var app = builder.Build();

var ahora = DateTime.UtcNow;
var taller1 = Guid.Parse("d0000000-0000-0000-0000-000000000001");
var proyecto1 = Guid.Parse("d0000000-0000-0000-0000-000000000003");
var ana = Guid.Parse("b0000000-0000-0000-0000-000000000001");
var luis = Guid.Parse("b0000000-0000-0000-0000-000000000002");

// Mismos identificadores que la semilla de Evaluaciones (EntregaTallerAna y EntregaTallerLuis).
List<EntregaDemo> entregas =
[
    new(Guid.Parse("e0000000-0000-0000-0000-000000000001"), taller1, ana, Redondear(ahora.AddDays(-1)), "ENVIADA", "taller1-ana.txt"),
    new(Guid.Parse("e0000000-0000-0000-0000-000000000002"), taller1, luis, Redondear(ahora.AddHours(-2)), "ENVIADA", "taller1-luis.txt"),
    new(Guid.Parse("e0000000-0000-0000-0000-000000000003"), proyecto1, ana, Redondear(ahora.AddDays(-4)), "ENVIADA", "proyecto1-ana.txt"),
    new(Guid.Parse("e0000000-0000-0000-0000-000000000004"), proyecto1, luis, Redondear(ahora.AddDays(-5)), "ENVIADA", "proyecto1-luis.txt"),
];

app.MapGet("/actividades/{actividadId:guid}/entregas", (Guid actividadId) =>
    Results.Ok(entregas.Where(e => e.ActividadId == actividadId).Select(Dto)));

app.MapGet("/mis-entregas", (HttpRequest request, Guid? actividadId) =>
{
    var estudiante = LeerSub(request);
    return Results.Ok(entregas
        .Where(e => e.EstudianteId == estudiante && (actividadId is null || e.ActividadId == actividadId))
        .Select(Dto));
});

app.MapGet("/entregas/{entregaId:guid}/archivo", (Guid entregaId) =>
{
    var entrega = entregas.FirstOrDefault(e => e.Id == entregaId);
    if (entrega is null)
        return Results.Json(new { status = 404, codigo = "NO_ENCONTRADO", mensaje = "La entrega no existe." }, statusCode: 404);

    var contenido = Encoding.UTF8.GetBytes(
        $"Archivo de demostración de la entrega {entrega.NombreArchivo}.\n" +
        "Lo genera el marcador de posición del servicio de entregas; el servicio real devolverá el archivo subido.\n");
    return Results.File(contenido, "text/plain; charset=utf-8", entrega.NombreArchivo);
});

app.MapHealthChecks("/health/live");

app.Run();

static object Dto(EntregaDemo e) => new
{
    id = e.Id,
    actividadId = e.ActividadId,
    estudianteId = e.EstudianteId,
    fechaEnvio = e.FechaEnvio,
    estado = e.Estado,
    nombreArchivo = e.NombreArchivo,
    tamano = 48_213
};

static DateTime Redondear(DateTime fecha) =>
    new(fecha.Ticks - fecha.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

// Lee el claim "sub" del JWT sin verificar la firma (lo verificó el gateway). Solo para este marcador.
static Guid? LeerSub(HttpRequest request)
{
    var encabezado = request.Headers.Authorization.ToString();
    if (!encabezado.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;

    var partes = encabezado["Bearer ".Length..].Trim().Split('.');
    if (partes.Length < 2) return null;

    try
    {
        var carga = partes[1].Replace('-', '+').Replace('_', '/');
        carga = carga.PadRight(carga.Length + (4 - carga.Length % 4) % 4, '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(carga));
        return json.RootElement.TryGetProperty("sub", out var sub) && Guid.TryParse(sub.GetString(), out var id) ? id : null;
    }
    catch (Exception ex) when (ex is FormatException or JsonException)
    {
        return null;
    }
}

internal sealed record EntregaDemo(
    Guid Id, Guid ActividadId, Guid EstudianteId, DateTime FechaEnvio, string Estado, string NombreArchivo);
