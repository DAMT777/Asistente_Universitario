namespace Entregas.Domain;

public enum EstadoEntrega
{
    Enviada,
    Anulada
}

public static class EstadoEntregaTexto
{
    /// <summary>Texto del contrato y de la base de datos (ENVIADA o ANULADA).</summary>
    public static string ComoTexto(this EstadoEntrega estado) => estado == EstadoEntrega.Enviada ? "ENVIADA" : "ANULADA";
}

/// <summary>
/// Entrega de un estudiante en una actividad (sección 9.4). Como máximo una por actividad y estudiante (RN-07).
/// El archivo no se guarda aquí: solo su ruta en el almacenamiento de objetos y sus metadatos.
/// ActividadId y EstudianteId referencian a otros servicios, sin llave foránea.
/// </summary>
public class Entrega
{
    public Guid Id { get; set; }
    public Guid ActividadId { get; set; }
    public Guid EstudianteId { get; set; }

    /// <summary>En UTC.</summary>
    public DateTime FechaEnvio { get; set; }

    public EstadoEntrega Estado { get; set; }
    public string NombreArchivo { get; set; } = "";
    public long Tamano { get; set; }

    /// <summary>{actividadId}/{estudianteId}/{guid}-{nombre} dentro del contenedor (sección 9.4).</summary>
    public string RutaBlob { get; set; } = "";

    public static string RutaPara(Guid actividadId, Guid estudianteId, string nombreArchivo) =>
        $"{actividadId}/{estudianteId}/{Guid.NewGuid():N}-{Path.GetFileName(nombreArchivo)}";
}
