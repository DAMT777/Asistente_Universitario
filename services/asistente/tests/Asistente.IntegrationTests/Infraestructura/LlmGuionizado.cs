using System.Collections.Concurrent;
using Asistente.Application.Puertos;

namespace Asistente.IntegrationTests.Infraestructura;

/// <summary>Doble de ILlmClient: devuelve respuestas guionizadas en orden y guarda lo que recibió.</summary>
public sealed class LlmGuionizado : ILlmClient
{
    private readonly ConcurrentQueue<Func<IReadOnlyList<MensajeLlm>, RespuestaLlm>> _guion = new();
    public List<(IReadOnlyList<MensajeLlm> Mensajes, IReadOnlyList<string> Herramientas)> Recibido { get; } = [];

    public void Reiniciar()
    {
        _guion.Clear();
        Recibido.Clear();
    }

    public LlmGuionizado Llamar(string herramienta, string argumentosJson = "{}")
    {
        _guion.Enqueue(_ => new RespuestaLlm(null, [new LlamadaHerramienta($"call_{Guid.NewGuid():N}", herramienta, argumentosJson)]));
        return this;
    }

    public LlmGuionizado Responder(string texto)
    {
        _guion.Enqueue(_ => RespuestaLlm.SoloTexto(texto));
        return this;
    }

    public Task<RespuestaLlm> CompletarAsync(IReadOnlyList<MensajeLlm> mensajes, IReadOnlyList<DefinicionHerramienta> herramientas, CancellationToken ct)
    {
        Recibido.Add((mensajes.ToList(), herramientas.Select(h => h.Nombre).ToList()));
        return Task.FromResult(_guion.TryDequeue(out var paso) ? paso(mensajes) : RespuestaLlm.SoloTexto("(fin del guion)"));
    }

    /// <summary>Contenido de los resultados de herramientas que el asistente le pasó al modelo.</summary>
    public IEnumerable<string> ResultadosHerramientas =>
        Recibido.SelectMany(r => r.Mensajes).Where(m => m.Rol == RolesMensaje.Herramienta).Select(m => m.Contenido ?? "").Distinct();
}
