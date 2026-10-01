using System.ComponentModel.DataAnnotations;

namespace Entregas.Infrastructure.Evaluaciones;

/// <summary>Variables Services__* y ServiceKey.</summary>
public sealed class ServiciosOpciones
{
    public const string Seccion = "Services";

    [Required, Url] public string EvaluacionesBaseUrl { get; set; } = "";
    [Range(1, 60)] public int EvaluacionesTimeoutSegundos { get; set; } = 3;
}

public sealed class ClaveServicioOpciones
{
    public const string Encabezado = "X-Service-Key";

    [Required, MinLength(16)] public string ServiceKey { get; set; } = "";
}
