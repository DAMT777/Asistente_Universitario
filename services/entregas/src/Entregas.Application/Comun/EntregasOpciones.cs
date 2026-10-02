using System.ComponentModel.DataAnnotations;
using Entregas.Domain;

namespace Entregas.Application.Comun;

/// <summary>Variables Entregas__*.</summary>
public sealed class EntregasOpciones
{
    public const string Seccion = "Entregas";

    /// <summary>Máximo por archivo (por defecto 20 MB).</summary>
    [Range(1, long.MaxValue)]
    public long MaxBytes { get; set; }

    /// <summary>Máximo de la suma de archivos de una entrega (por defecto 50 MB).</summary>
    [Range(1, long.MaxValue)]
    public long MaxBytesTotal { get; set; }

    /// <summary>Máximo de archivos por entrega (por defecto 10).</summary>
    [Range(1, 100)]
    public int MaxArchivos { get; set; }

    public LimitesEntrega Limites => new(MaxBytes, MaxBytesTotal, MaxArchivos);
}
