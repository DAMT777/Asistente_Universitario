using System.ComponentModel.DataAnnotations;

namespace Asistente.Infrastructure.Llm;

/// <summary>Variables Llm__*. La llave solo llega por entorno (infra/.env); nunca está en el código.</summary>
public sealed class LlmOpciones
{
    public const string Seccion = "Llm";

    [Required, Url] public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    [Required] public string ApiKey { get; set; } = "";
    [Required] public string Model { get; set; } = "";
    [Range(1, 120)] public int TimeoutSeconds { get; set; } = 15;
}
