using Asistente.Application;
using Asistente.Application.CasosDeUso;
using Asistente.Application.Herramientas;
using Asistente.Application.Puertos;
using Asistente.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Asistente.UnitTests;

public sealed class SeguridadDelTurnoTests
{
    [Fact]
    public async Task Una_respuesta_de_herramienta_no_puede_originar_una_propuesta()
    {
        var auditoria = new AuditoriaEnMemoria();
        var catalogo = new CatalogoHerramientas([
            new HerramientaFalsa("leer", UsuarioAsistente.Profesor, false),
            new HerramientaFalsa("proponer", UsuarioAsistente.Profesor, true)
        ]);
        var procesar = new ProcesarMensaje(new ModeloGuionizado(), catalogo, auditoria,
            TimeProvider.System, Options.Create(new AsistenteOpciones()), NullLogger<ProcesarMensaje>.Instance);

        var resultado = await procesar.EjecutarAsync(new(Guid.NewGuid(), UsuarioAsistente.Profesor),
            new(null, "Consulta mi curso"), CancellationToken.None);

        Assert.Null(resultado.AccionPendiente);
        Assert.Single(auditoria.Registros);
    }

    private sealed class ModeloGuionizado : ILlmClient
    {
        private int turno;

        public Task<RespuestaLlm> CompletarAsync(IReadOnlyList<MensajeLlm> mensajes,
            IReadOnlyList<DefinicionHerramienta> herramientas, CancellationToken ct)
        {
            if (turno++ == 0)
                return Task.FromResult(new RespuestaLlm(null, [new("1", "leer", "{}")]));

            Assert.DoesNotContain(herramientas, h => h.Nombre == "proponer");
            return Task.FromResult(turno == 2
                ? new RespuestaLlm(null, [new("2", "proponer", "{}")])
                : RespuestaLlm.SoloTexto("Listo."));
        }
    }
}
