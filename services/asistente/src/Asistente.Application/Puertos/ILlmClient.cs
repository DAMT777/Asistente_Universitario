using System.Text.Json;

namespace Asistente.Application.Puertos;

/// <summary>
/// Modelo de lenguaje con llamadas a herramientas. La implementación vive en Infrastructure:
/// ninguna otra clase sabe qué proveedor hay detrás.
/// </summary>
public interface ILlmClient
{
    /// <exception cref="Asistente.Domain.ExcepcionAsistente">ASISTENTE_NO_DISPONIBLE si el proveedor no responde a tiempo o limita (429).</exception>
    Task<RespuestaLlm> CompletarAsync(IReadOnlyList<MensajeLlm> mensajes, IReadOnlyList<DefinicionHerramienta> herramientas, CancellationToken ct);
}

public static class RolesMensaje
{
    public const string Sistema = "system";
    public const string Usuario = "user";
    public const string Asistente = "assistant";
    public const string Herramienta = "tool";
}

/// <param name="LlamadaId">Para mensajes de rol tool: id de la llamada que responde.</param>
public sealed record MensajeLlm(string Rol, string? Contenido, IReadOnlyList<LlamadaHerramienta>? Llamadas = null, string? LlamadaId = null);

public sealed record LlamadaHerramienta(string Id, string Nombre, string ArgumentosJson);

public sealed record RespuestaLlm(string? Texto, IReadOnlyList<LlamadaHerramienta> Llamadas)
{
    public static RespuestaLlm SoloTexto(string texto) => new(texto, []);
}

/// <param name="Parametros">JSON Schema de los argumentos.</param>
public sealed record DefinicionHerramienta(string Nombre, string Descripcion, JsonElement Parametros);
