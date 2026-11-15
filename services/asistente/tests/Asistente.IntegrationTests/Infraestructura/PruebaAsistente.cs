using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Asistente.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Unillanos.Pruebas.Compartidas;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Asistente.IntegrationTests.Infraestructura;

/// <summary>
/// Base: limpia la base, el guion del modelo y los stubs del gateway. Los stubs reproducen las respuestas
/// reales de Evaluaciones y Entregas con los datos semilla (SemillaDesarrollo.cs).
/// </summary>
[Collection(ColeccionAsistente.Nombre)]
public abstract class PruebaAsistente(AsistenteFixture fixture) : IAsyncLifetime
{
    protected static readonly Guid Curso = UsuariosSemilla.Curso;
    protected static readonly Guid Taller1 = UsuariosSemilla.Taller1;
    protected const string VersionLuis = "AAAAAAAAB9E=";

    protected AsistenteFixture Fixture { get; } = fixture;
    protected virtual AsistenteApiFactory Api => Fixture.Api;
    protected LlmGuionizado Llm => Api.Llm;
    protected WireMockServer Gateway => Fixture.Gateway;

    public virtual async ValueTask InitializeAsync()
    {
        Api.Reloj.Ahora = RelojFijo.AhoraTruncado();
        Llm.Reiniciar();
        Gateway.Reset();
        Fixture.Groq.Reset();
        StubsSemilla();
        await using var scope = Api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AsistenteDbContext>();
        await db.Auditoria.ExecuteDeleteAsync();
        await db.Acciones.ExecuteDeleteAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    protected string TokenDe(Guid id, string rol, bool expirado = false) => Fixture.Llaves.CrearToken(id, rol, "Usuario", expirado);

    protected HttpClient Cliente(string? token)
    {
        var c = Api.CreateClient();
        if (token is not null) c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    protected string TokenAna => TokenDe(UsuariosSemilla.Ana, "ESTUDIANTE");
    protected string TokenLuis => TokenDe(UsuariosSemilla.Luis, "ESTUDIANTE");
    protected string TokenProfesor => TokenDe(UsuariosSemilla.Profesor, "PROFESOR");

    protected static Task<HttpResponseMessage> EnviarAsync(HttpClient c, string mensaje) =>
        c.PostAsJsonAsync("/asistente/mensajes", new { mensaje });

    protected static async Task<RespuestaDto> LeerAsync(HttpResponseMessage r)
    {
        Assert.Equal(System.Net.HttpStatusCode.OK, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<RespuestaDto>())!;
    }

    protected static async Task<ErrorDto> ErrorAsync(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<ErrorDto>())!;

    protected void Stub(string metodo, string ruta, int estado, object? cuerpo)
    {
        var r = Response.Create().WithStatusCode(estado);
        if (cuerpo is not null) r = r.WithHeader("Content-Type", estado >= 400 ? "application/problem+json" : "application/json").WithBodyAsJson(cuerpo);
        Gateway.Given(Request.Create().WithPath(ruta).UsingMethod(metodo)).RespondWith(r);
    }

    /// <summary>Ana ve sus notas publicadas. Para Luis, Evaluaciones ya devuelve el borrador como SIN_CALIFICAR.</summary>
    private void StubsSemilla()
    {
        Stub("GET", "/mis-notas", 200, new
        {
            cursos = new[]
            {
                new { cursoId = Curso, codigo = "603803", nombre = "Simulación computacional", profesor = "Profesor Demo",
                      cortes = new object[] { new { corte = 1, pesoCorte = 30m, nota = (decimal?)3.2m, publicado = true }, new { corte = 2, pesoCorte = 30m, nota = (decimal?)null, publicado = false } },
                      definitivaParcial = 0.96m, esParcial = true },
            },
        });
        Stub("GET", "/mis-entregas", 200, Array.Empty<object>());
        Stub("GET", "/cursos", 200, new[] { new { id = Curso, codigo = "603803", nombre = "Simulación computacional", profesor = "Profesor Demo" } });
        Stub("GET", $"/cursos/{Curso}/estudiantes", 200, new[]
        {
            new { id = UsuariosSemilla.Ana, nombre = "Ana Demo", codigo = "E0001", rol = "ESTUDIANTE" },
            new { id = UsuariosSemilla.Luis, nombre = "Luis Demo", codigo = "E0002", rol = "ESTUDIANTE" },
            new { id = UsuariosSemilla.Marta, nombre = "Marta Demo", codigo = "E0003", rol = "ESTUDIANTE" },
        });
        Stub("GET", $"/cursos/{Curso}/actividades", 200, new[]
        {
            new { id = Taller1, titulo = "Taller 1", corte = 1, peso = 20m },
            new { id = UsuariosSemilla.Parcial1, titulo = "Parcial 1", corte = 1, peso = 80m },
            new { id = UsuariosSemilla.Proyecto1, titulo = "Proyecto 1", corte = 2, peso = 100m },
        });
        Stub("GET", $"/actividades/{Taller1}/calificaciones", 200, new[]
        {
            new { estudianteId = UsuariosSemilla.Ana, valor = 4.0m, estado = "PUBLICADA", version = "AAAAAAAAB9A=" },
            new { estudianteId = UsuariosSemilla.Luis, valor = 2.5m, estado = "BORRADOR", version = VersionLuis },
        });
        Stub("PUT", $"/actividades/{Taller1}/calificaciones/{UsuariosSemilla.Luis}", 200, new
        {
            id = Guid.NewGuid(), actividadId = Taller1, estudianteId = UsuariosSemilla.Luis, entregaId = (Guid?)null,
            valor = 4.2m, retroalimentacion = "Mejoró", estado = "BORRADOR", version = "AAAAAAAAB9I=",
        });
    }

    protected void NotasDeCurso(params object[] actividades) =>
        Stub("GET", $"/mis-notas/cursos/{Curso}/actividades", 200, new { cursoId = Curso, actividades });

    protected IReadOnlyList<ILogEntry> Escrituras => Gateway.LogEntries.Where(e => e.RequestMessage!.Method! != "GET").ToList();

    protected async Task<(List<Asistente.Domain.AccionPendiente> Acciones, List<Asistente.Domain.RegistroAuditoria> Auditoria)> BaseAsync()
    {
        await using var scope = Api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AsistenteDbContext>();
        return (await db.Acciones.AsNoTracking().ToListAsync(), await db.Auditoria.AsNoTracking().ToListAsync());
    }

    /// <summary>El profesor pide la nota de Luis y queda una acción pendiente.</summary>
    protected async Task<AccionDto> ProponerNotaLuisAsync()
    {
        Llm.Llamar("proponer_calificacion", """{"actividad":"Taller 1","estudiante":"Luis","valor":4.2,"retroalimentacion":"Mejoró"}""")
           .Responder("Te propongo poner 4.2 a Luis Demo en Taller 1. Confírmalo para guardarlo.");
        var r = await LeerAsync(await EnviarAsync(Cliente(TokenProfesor), "Ponle 4.2 a Luis en el Taller 1, mejoró"));
        return r.AccionPendiente ?? throw new InvalidOperationException("No se creó la acción.");
    }

    public sealed record AccionDto(Guid Id, string Resumen, DateTimeOffset ExpiraEn);
    public sealed record RespuestaDto(Guid ConversacionId, string Respuesta, AccionDto? AccionPendiente);
    public sealed record ErrorDto(int Status, string Codigo, string Mensaje, string TraceId);

    protected static JsonElement Json(string s) => JsonDocument.Parse(s).RootElement;
}
