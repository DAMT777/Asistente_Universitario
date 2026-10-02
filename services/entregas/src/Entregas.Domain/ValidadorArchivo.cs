namespace Entregas.Domain;

/// <summary>
/// Valida un archivo de entrega por tamaño, extensión y contenido (firma).
/// La extensión sola no basta: un .exe renombrado a .pdf se rechaza.
/// </summary>
public static class ValidadorArchivo
{
    public const int LongitudMaximaNombre = 255;

    public static TipoArchivo Validar(string nombreOriginal, long tamano, ReadOnlySpan<byte> cabecera, long maxBytes)
    {
        if (string.IsNullOrWhiteSpace(nombreOriginal) || nombreOriginal.Length > LongitudMaximaNombre)
            throw new ArchivoInvalidoException($"El nombre del archivo es obligatorio y admite hasta {LongitudMaximaNombre} caracteres.");
        if (tamano <= 0)
            throw new ArchivoInvalidoException($"'{nombreOriginal}' está vacío.");
        if (tamano > maxBytes)
            throw new ArchivoDemasiadoGrandeException($"'{nombreOriginal}' supera el máximo de {LimitesEntrega.Megas(maxBytes)} por archivo.");

        var extension = Path.GetExtension(nombreOriginal).ToLowerInvariant();
        var tipo = TipoArchivo.Permitidos.FirstOrDefault(t => t.Extensiones.Contains(extension))
                   ?? throw new TipoArchivoNoPermitidoException($"'{nombreOriginal}': la extensión '{extension}' no está permitida.");

        if (!tipo.CoincideFirma(cabecera))
            throw new TipoArchivoNoPermitidoException($"El contenido de '{nombreOriginal}' no corresponde a un {tipo.Nombre}.");

        return tipo;
    }
}
