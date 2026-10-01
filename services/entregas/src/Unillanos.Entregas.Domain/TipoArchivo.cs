namespace Unillanos.Entregas.Domain;

/// <summary>Tipo de archivo permitido, con sus extensiones y firma (magic bytes).</summary>
public sealed record TipoArchivo(string Nombre, string ContentType, IReadOnlyList<string> Extensiones, IReadOnlyList<byte[]> Firmas)
{
    private static readonly byte[] FirmaPdf = "%PDF"u8.ToArray();
    private static readonly byte[] FirmaOle = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
    private static readonly byte[] FirmaZip = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] FirmaZipVacio = [0x50, 0x4B, 0x05, 0x06];
    private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] FirmaJpg = [0xFF, 0xD8, 0xFF];

    public static readonly IReadOnlyList<TipoArchivo> Permitidos =
    [
        new("PDF", "application/pdf", [".pdf"], [FirmaPdf]),
        new("DOC", "application/msword", [".doc"], [FirmaOle]),
        new("XLS", "application/vnd.ms-excel", [".xls"], [FirmaOle]),
        new("PPT", "application/vnd.ms-powerpoint", [".ppt"], [FirmaOle]),
        new("DOCX", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", [".docx"], [FirmaZip]),
        new("XLSX", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [".xlsx"], [FirmaZip]),
        new("PPTX", "application/vnd.openxmlformats-officedocument.presentationml.presentation", [".pptx"], [FirmaZip]),
        new("ZIP", "application/zip", [".zip"], [FirmaZip, FirmaZipVacio]),
        new("PNG", "image/png", [".png"], [FirmaPng]),
        new("JPG", "image/jpeg", [".jpg", ".jpeg"], [FirmaJpg]),
    ];

    /// <summary>Bytes de cabecera necesarios para reconocer cualquier firma.</summary>
    public static int LongitudCabecera { get; } = Permitidos.SelectMany(t => t.Firmas).Max(f => f.Length);

    public bool CoincideFirma(ReadOnlySpan<byte> cabecera)
    {
        foreach (var firma in Firmas)
        {
            if (cabecera.StartsWith(firma)) return true;
        }
        return false;
    }
}
