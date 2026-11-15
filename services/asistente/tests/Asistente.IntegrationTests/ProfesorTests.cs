using System.Net;
using System.Net.Http.Json;
using Asistente.Domain;
using Asistente.IntegrationTests.Infraestructura;
using Unillanos.Pruebas.Compartidas;

namespace Asistente.IntegrationTests;

public sealed class ProfesorTests(AsistenteFixture fixture) : PruebaAsistente(fixture)
{
    private static string Confirmar(Guid id) => $"/asistente/acciones/{id}/confirmar";

    [Fact]
    public async Task Proponer_crea_la_accion_y_no_escribe_en_evaluaciones()
    {
        var accion = await ProponerNotaLuisAsync();

        Assert.Contains("Luis Demo", accion.Resumen);
        Assert.Contains("4.2", accion.Resumen);
        Assert.Equal(Api.Reloj.Ahora.AddMinutes(5), accion.ExpiraEn);
        Assert.Empty(Escrituras);
        var fila = Assert.Single((await BaseAsync()).Acciones);
        Assert.Equal(UsuariosSemilla.Profesor, fila.CreadaPor);
        Assert.Equal(UsuariosSemilla.Luis, fila.EstudianteId);
        Assert.Equal(2.5m, fila.ValorAnterior);
        Assert.Equal(EstadoAccion.Pendiente, fila.Estado);
    }

    [Fact]
    public async Task Varias_coincidencias_pregunta_en_vez_de_adivinar()
    {
        Llm.Llamar("proponer_calificacion", """{"actividad":"Taller 1","estudiante":"Demo","valor":4.0}""").Responder("¿A cuál estudiante?");

        var r = await LeerAsync(await EnviarAsync(Cliente(TokenProfesor), "Ponle 4 al de Demo"));

        Assert.Null(r.AccionPendiente);
        Assert.Contains("varias coincidencias", Assert.Single(Llm.ResultadosHerramientas));
        Assert.Empty((await BaseAsync()).Acciones);
    }

    [Fact]
    public async Task Confirmar_escribe_una_sola_vez_con_el_jwt_del_profesor()
    {
        var accion = await ProponerNotaLuisAsync();
        var token = TokenProfesor;
        var cliente = Cliente(token);

        var r = await cliente.PostAsync(Confirmar(accion.Id), null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var cuerpo = Json(await r.Content.ReadAsStringAsync());
        Assert.Equal("BORRADOR", cuerpo.GetProperty("estado").GetString());
        var put = Assert.Single(Escrituras);
        Assert.Equal("PUT", put.RequestMessage!.Method);
        Assert.Equal($"/actividades/{Taller1}/calificaciones/{UsuariosSemilla.Luis}", put.RequestMessage!.Path);
        Assert.Equal($"Bearer {token}", put.RequestMessage!.Headers!["Authorization"].Single());
        Assert.Equal($"\"{VersionLuis}\"", put.RequestMessage!.Headers!["If-Match"].Single());
        Assert.Contains("4.2", put.RequestMessage!.Body!);

        var otra = await cliente.PostAsync(Confirmar(accion.Id), null);
        Assert.Equal(HttpStatusCode.Conflict, otra.StatusCode);
        Assert.Equal("ACCION_YA_USADA", (await ErrorAsync(otra)).Codigo);
        Assert.Single(Escrituras);
    }

    [Fact]
    public async Task Confirmar_despues_de_expirar_da_410()
    {
        var accion = await ProponerNotaLuisAsync();
        Api.Reloj.Ahora = Api.Reloj.Ahora.AddMinutes(6);

        var r = await Cliente(TokenProfesor).PostAsync(Confirmar(accion.Id), null);

        Assert.Equal(HttpStatusCode.Gone, r.StatusCode);
        Assert.Equal("ACCION_EXPIRADA", (await ErrorAsync(r)).Codigo);
        Assert.Empty(Escrituras);
    }

    [Fact]
    public async Task Confirmar_con_otro_usuario_da_404()
    {
        var accion = await ProponerNotaLuisAsync();
        var otroProfesor = TokenDe(Guid.NewGuid(), "PROFESOR");

        var r = await Cliente(otroProfesor).PostAsync(Confirmar(accion.Id), null);
        var estudiante = await Cliente(TokenLuis).PostAsync(Confirmar(accion.Id), null);

        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        Assert.Equal("ACCION_NO_ENCONTRADA", (await ErrorAsync(r)).Codigo);
        Assert.Equal(HttpStatusCode.NotFound, estudiante.StatusCode);
        Assert.Empty(Escrituras);
    }

    [Fact]
    public async Task Conflicto_de_version_en_evaluaciones_se_devuelve_y_no_es_exito()
    {
        var accion = await ProponerNotaLuisAsync();
        Stub("PUT", $"/actividades/{Taller1}/calificaciones/{UsuariosSemilla.Luis}", 409,
            new { status = 409, codigo = "CONFLICTO_CONCURRENCIA", mensaje = "La calificación cambió.", traceId = "x" });

        var r = await Cliente(TokenProfesor).PostAsync(Confirmar(accion.Id), null);

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        Assert.Equal("CONFLICTO_CONCURRENCIA", (await ErrorAsync(r)).Codigo);
        var (acciones, auditoria) = await BaseAsync();
        Assert.Equal(EstadoAccion.Fallida, Assert.Single(acciones).Estado);
        Assert.Contains(auditoria, a => a.Accion == AccionesAuditoria.Confirmar && a.Resultado == "ERROR:CONFLICTO_CONCURRENCIA");
        Assert.DoesNotContain(auditoria, a => a.Resultado == "EXITO");
    }

    [Fact]
    public async Task Cancelar_deja_la_accion_inutilizable()
    {
        var accion = await ProponerNotaLuisAsync();
        var cliente = Cliente(TokenProfesor);

        var r = await cliente.PostAsync($"/asistente/acciones/{accion.Id}/cancelar", null);
        var confirmar = await cliente.PostAsync(Confirmar(accion.Id), null);

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal("CANCELADA", Json(await r.Content.ReadAsStringAsync()).GetProperty("estado").GetString());
        Assert.Equal(HttpStatusCode.Conflict, confirmar.StatusCode);
        Assert.Empty(Escrituras);
    }

    [Fact]
    public async Task Se_audita_cada_confirmacion_sin_el_token()
    {
        var accion = await ProponerNotaLuisAsync();
        var token = TokenProfesor;
        await Cliente(token).PostAsync(Confirmar(accion.Id), null);

        var auditoria = (await BaseAsync()).Auditoria;
        var confirmacion = Assert.Single(auditoria, a => a.Accion == AccionesAuditoria.Confirmar);
        Assert.Equal(UsuariosSemilla.Profesor, confirmacion.UsuarioId);
        Assert.Equal("PROFESOR", confirmacion.Rol);
        Assert.Equal("EXITO", confirmacion.Resultado);
        Assert.Equal("asistente", confirmacion.Origen);
        Assert.Contains("2.5", confirmacion.DatosAntes);
        Assert.Contains("4.2", confirmacion.DatosDespues);
        Assert.Equal(TimeSpan.Zero, confirmacion.FechaUtc.Offset);
        var firma = token.Split('.')[2];
        Assert.All(auditoria, a => Assert.DoesNotContain(firma, $"{a.DatosAntes}{a.DatosDespues}{a.Resultado}"));
        Assert.All(auditoria, a => Assert.DoesNotContain("Bearer", $"{a.DatosAntes}{a.DatosDespues}"));
    }

    [Fact]
    public async Task Nunca_envia_llave_de_servicio_ni_llama_rutas_internas()
    {
        var accion = await ProponerNotaLuisAsync();
        await Cliente(TokenProfesor).PostAsync(Confirmar(accion.Id), null);
        Llm.Llamar("listar_estudiantes_curso").Responder("Ana, Luis y Marta.");
        await EnviarAsync(Cliente(TokenProfesor), "¿Quiénes están inscritos?");

        Assert.NotEmpty(Gateway.LogEntries);
        Assert.All(Gateway.LogEntries, e =>
        {
            Assert.DoesNotContain("/internal", e.RequestMessage!.Path!);
            Assert.False(e.RequestMessage!.Headers!.ContainsKey("X-Service-Key"));
            Assert.StartsWith("Bearer ", e.RequestMessage!.Headers!["Authorization"].Single());
        });
    }
}
