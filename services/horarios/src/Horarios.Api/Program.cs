using System.Net.Http.Headers;
using System.Text.Json;
using Unillanos.ServiceDefaults;
using Unillanos.ServiceDefaults.Errores;
using Unillanos.ServiceDefaults.Seguridad;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var baseUrl = builder.Configuration["Evaluaciones:BaseUrl"] ?? "http://localhost:5002/";
builder.Services.AddHttpClient("evaluaciones", client =>
{
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

var bloques = builder.Configuration.GetSection("Horarios:Bloques").Get<BloqueConfigurado[]>() ?? [];
if (bloques.Any(b => b.CursoId == Guid.Empty || b.Dia is < 1 or > 6 || b.Inicio < new TimeOnly(6, 0)
    || b.Fin > new TimeOnly(18, 0) || b.Fin <= b.Inicio
    || b.Inicio.Second != 0 || b.Fin.Second != 0 || string.IsNullOrWhiteSpace(b.Aula)))
    throw new InvalidOperationException("Los bloques de horario requieren curso, aula y un intervalo válido entre las 06:00 y las 18:00, de lunes a sábado.");

var app = builder.Build();
app.UseServiceDefaults();

app.MapGet("/horarios/semana", async (HttpContext context, IHttpClientFactory clientes, CancellationToken ct) =>
{
    // Evaluaciones aplica las inscripciones del estudiante o la titularidad del profesor.
    using var solicitud = new HttpRequestMessage(HttpMethod.Get, "cursos");
    solicitud.Headers.Authorization = AuthenticationHeaderValue.Parse(context.Request.Headers.Authorization.ToString());
    var cliente = clientes.CreateClient("evaluaciones");
    CursoAutorizado[] cursos;
    try
    {
        using var respuesta = await cliente.SendAsync(solicitud, ct);
        respuesta.EnsureSuccessStatusCode();
        cursos = await respuesta.Content.ReadFromJsonAsync<CursoAutorizado[]>(ct) ?? [];
    }
    catch (Exception e) when ((e is HttpRequestException or JsonException or OperationCanceledException) && !ct.IsCancellationRequested)
    {
        await EscritorErrores.EscribirAsync(context, "HORARIOS_NO_DISPONIBLES", "No fue posible consultar tus cursos. Intenta de nuevo.", 503);
        return;
    }

    var accesibles = cursos.ToDictionary(c => c.Id);
    var horario = bloques.Where(b => accesibles.ContainsKey(b.CursoId))
        .OrderBy(b => b.Dia).ThenBy(b => b.Inicio)
        .Select(b => new
        {
            b.CursoId, accesibles[b.CursoId].Nombre, accesibles[b.CursoId].Codigo, b.Dia,
            Inicio = b.Inicio.ToString("HH:mm"), Fin = b.Fin.ToString("HH:mm"), b.Aula,
        });
    await context.Response.WriteAsJsonAsync(horario, ct);
}).RequireAuthorization(Politicas.EstudianteOProfesor);

await app.RunAsync();

internal sealed record BloqueConfigurado(Guid CursoId, int Dia, TimeOnly Inicio, TimeOnly Fin, string Aula);
internal sealed record CursoAutorizado(Guid Id, string Nombre, string Codigo);
