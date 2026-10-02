namespace Entregas.Domain;

/// <summary>
/// Límites de una entrega: tamaño por archivo, tamaño total y cantidad de archivos.
/// Los valores salen de configuración (Entregas__MaxBytes, Entregas__MaxBytesTotal, Entregas__MaxArchivos).
/// </summary>
public sealed record LimitesEntrega(long MaxBytesPorArchivo, long MaxBytesTotal, int MaxArchivos)
{
    /// <summary>Reglas del conjunto. Cada archivo se valida además con <see cref="ValidadorArchivo"/>.</summary>
    public void ValidarConjunto(IReadOnlyCollection<long> tamanos)
    {
        if (tamanos.Count == 0)
            throw new ArchivoInvalidoException("Adjunte al menos un archivo.");
        if (tamanos.Count > MaxArchivos)
            throw new ArchivoInvalidoException($"Se admiten como máximo {MaxArchivos} archivos por entrega.");
        if (tamanos.Sum() > MaxBytesTotal)
            throw new ArchivoDemasiadoGrandeException($"Los archivos suman más del máximo de {Megas(MaxBytesTotal)} por entrega.");
    }

    /// <summary>Formato fijo (punto decimal) sin depender de la cultura del servidor.</summary>
    public static string Megas(long bytes) => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{bytes / (1024d * 1024d):0.#} MB");
}
