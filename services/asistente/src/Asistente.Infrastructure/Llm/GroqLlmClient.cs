using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Asistente.Application.Puertos;
using Asistente.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Asistente.Infrastructure.Llm;

/// <summary>
/// Cliente del API compatible con OpenAI de Groq (POST {BaseUrl}/chat/completions con tools).
/// HttpClient tipado: sin paquetes extra. No reintenta: un 429, un 5xx o el timeout se informan como
/// ASISTENTE_NO_DISPONIBLE (503).
/// </summary>
public sealed class GroqLlmClient(HttpClient http, IOptions<LlmOpciones> opciones, ILogger<GroqLlmClient> logger) : ILlmClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<RespuestaLlm> CompletarAsync(IReadOnlyList<MensajeLlm> mensajes, IReadOnlyList<DefinicionHerramienta> herramientas, CancellationToken ct)
    {
        var cuerpo = new Solicitud(
            opciones.Value.Model,
            mensajes.Select(m => new Mensaje(
                m.Rol,
                m.Contenido ?? (m.Llamadas is { Count: > 0 } ? null : ""),
                m.Llamadas?.Select(l => new LlamadaTool(l.Id, "function", new Funcion(l.Nombre, l.ArgumentosJson))).ToList(),
                m.LlamadaId)).ToList(),
            herramientas.Count == 0 ? null : herramientas.Select(h => new Tool("function", new DefFuncion(h.Nombre, h.Descripcion, h.Parametros))).ToList(),
            herramientas.Count == 0 ? null : "auto",
            0.1, 768);

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.PostAsJsonAsync("chat/completions", cuerpo, Json, ct);
        }
        catch (Exception e) when (e is TaskCanceledException or HttpRequestException && !ct.IsCancellationRequested)
        {
            logger.LogWarning("El proveedor de IA no respondió: {Tipo}", e.GetType().Name);
            throw NoDisponible();
        }

        using (respuesta)
        {
            if (respuesta.StatusCode == HttpStatusCode.TooManyRequests || (int)respuesta.StatusCode >= 500)
            {
                logger.LogWarning("El proveedor de IA respondió {Estado}", (int)respuesta.StatusCode);
                throw NoDisponible();
            }
            if (!respuesta.IsSuccessStatusCode)
            {
                // 400/401/404: configuración (modelo o llave). No se registra el cuerpo para no filtrar datos.
                logger.LogError("El proveedor de IA rechazó la solicitud con {Estado}", (int)respuesta.StatusCode);
                throw NoDisponible();
            }

            var datos = await respuesta.Content.ReadFromJsonAsync<Respuesta>(Json, ct);
            var mensaje = datos?.Choices?.FirstOrDefault()?.Message;
            if (mensaje is null) throw NoDisponible();
            return new RespuestaLlm(
                mensaje.Content,
                mensaje.ToolCalls?.Select(t => new LlamadaHerramienta(t.Id, t.Function.Name, t.Function.Arguments ?? "{}")).ToList() ?? []);
        }
    }

    private static ExcepcionAsistente NoDisponible() =>
        new(CodigosAsistente.AsistenteNoDisponible, "El asistente no está disponible en este momento. Intenta de nuevo en unos minutos.");

    public static void Configurar(HttpClient http, LlmOpciones o)
    {
        http.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", o.ApiKey);
    }

    private sealed record Solicitud(string Model, List<Mensaje> Messages, List<Tool>? Tools, string? ToolChoice, double Temperature, int MaxCompletionTokens);
    private sealed record Mensaje(string Role, string? Content, List<LlamadaTool>? ToolCalls, string? ToolCallId);
    private sealed record LlamadaTool(string Id, string Type, Funcion Function);
    private sealed record Funcion(string Name, string? Arguments);
    private sealed record Tool(string Type, DefFuncion Function);
    private sealed record DefFuncion(string Name, string Description, JsonElement Parameters);
    private sealed record Respuesta(List<Opcion>? Choices);
    private sealed record Opcion(MensajeRespuesta? Message);
    private sealed record MensajeRespuesta(string? Content, List<LlamadaTool>? ToolCalls);
}
