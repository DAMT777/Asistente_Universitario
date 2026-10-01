using System.ComponentModel.DataAnnotations;

namespace Unillanos.Entregas.Application.Comun;

/// <summary>Variables Entregas__*.</summary>
public sealed class EntregasOpciones
{
    public const string Seccion = "Entregas";

    [Range(1, long.MaxValue)]
    public long MaxBytes { get; set; }
}
