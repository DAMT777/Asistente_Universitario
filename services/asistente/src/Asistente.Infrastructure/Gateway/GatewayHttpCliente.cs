using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Asistente.Application.Puertos;
using Asistente.Domain;

namespace Asistente.Infrastructure.Gateway;

/// <summary>
/// Llama a las rutas PÚBLICAS de Evaluaciones y Entregas a través del gateway. Nunca rutas internas ni llave de servicio.
/// Las lecturas se reintentan una vez ante 5xx o timeout; la escritura nunca.
/// </summary>
public sealed class GatewayHttpCliente(HttpClient http) : IGatewayCliente
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<MatrizNotas> MisNotasAsync(CancellationToken ct) => LeerAsync<MatrizNotas>("mis-notas", ct);
    public Task<NotasCurso> MisNotasDeCursoAsync(Guid cursoId, CancellationToken ct) => LeerAsync<NotasCurso>($"mis-notas/cursos/{cursoId}/actividades", ct);
    public Task<IReadOnlyList<EntregaPropia>> MisEntregasAsync(CancellationToken ct) => LeerAsync<IReadOnlyList<EntregaPropia>>("mis-entregas", ct);
    public Task<IReadOnlyList<CursoProfesor>> CursosAsync(CancellationToken ct) => LeerAsync<IReadOnlyList<CursoProfesor>>("cursos", ct);
    public Task<IReadOnlyList<EstudianteInscrito>> EstudiantesDelCursoAsync(Guid cursoId, CancellationToken ct) => LeerAsync<IReadOnlyList<EstudianteInscrito>>($"cursos/{cursoId}/estudiantes", ct);
    public Task<IReadOnlyList<ActividadCurso>> ActividadesDelCursoAsync(Guid cursoId, CancellationToken ct) => LeerAsync<IReadOnlyList<ActividadCurso>>($"cursos/{cursoId}/actividades", ct);
    public Task<IReadOnlyList<CalificacionActividad>> CalificacionesDeActividadAsync(Guid actividadId, CancellationToken ct) => LeerAsync<IReadOnlyList<CalificacionActividad>>($"actividades/{actividadId}/calificaciones", ct);

    public async Task<JsonElement> CalificarAsync(Guid actividadId, Guid estudianteId, decimal valor, string? retroalimentacion, string? versionEsperada, CancellationToken ct)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Put, $"actividades/{actividadId}/calificaciones/{estudianteId}")
        {
            Content = JsonContent.Create(new { valor, retroalimentacion }, options: Json),
        };
        if (versionEsperada is not null)
            solicitud.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{versionEsperada}\""));

        using var respuesta = await http.SendAsync(solicitud, ct);
        await LanzarSiErrorAsync(respuesta, ct);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>(Json, ct);
    }

    private async Task<T> LeerAsync<T>(string ruta, CancellationToken ct)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                using var respuesta = await http.GetAsync(ruta, ct);
                if ((int)respuesta.StatusCode >= 500 && intento == 1) continue;
                await LanzarSiErrorAsync(respuesta, ct);
                return (await respuesta.Content.ReadFromJsonAsync<T>(Json, ct))!;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                if (intento == 1) continue;
                throw new ExcepcionAsistente(CodigosAsistente.ServicioNoDisponible, "Un servicio necesario no respondió a tiempo.", 503);
            }
        }
    }

    private static async Task LanzarSiErrorAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode) return;
        string codigo = "ERROR_REMOTO", mensaje = "El servicio rechazó la solicitud.";
        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<JsonElement>(Json, ct);
            if (error.TryGetProperty("codigo", out var c) && c.GetString() is { } cs) codigo = cs;
            if (error.TryGetProperty("mensaje", out var m) && m.GetString() is { } ms) mensaje = ms;
        }
        catch (JsonException) { }
        if (respuesta.StatusCode == HttpStatusCode.ServiceUnavailable && codigo == "ERROR_REMOTO") codigo = CodigosAsistente.ServicioNoDisponible;
        throw new ErrorRemotoException((int)respuesta.StatusCode, codigo, mensaje);
    }
}
