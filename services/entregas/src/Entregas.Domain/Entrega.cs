namespace Entregas.Domain;

public enum EstadoEntrega
{
    ENVIADA,
    ANULADA,
}

/// <summary>
/// Una entrega por actividad y estudiante (RN-07), con uno o varios archivos.
/// Editar reemplaza el conjunto de archivos; anular cambia el estado;
/// volver a subir tras anular reutiliza la fila (DA-05).
/// </summary>
public sealed class Entrega
{
    private readonly List<ArchivoEntrega> _archivos = [];

    public Guid Id { get; private set; }
    public Guid ActividadId { get; private set; }
    public Guid EstudianteId { get; private set; }
    public DateTime FechaEnvio { get; private set; }
    public EstadoEntrega Estado { get; private set; }
    /// <summary>En el orden en que se subieron.</summary>
    public IReadOnlyList<ArchivoEntrega> Archivos => _archivos.OrderBy(a => a.Orden).ToList();
    public long TamanoTotal => _archivos.Sum(a => a.Tamano);

    private Entrega() { }

    public static Entrega Crear(Guid actividadId, Guid estudianteId, IReadOnlyCollection<ArchivoGuardado> archivos, DateTimeOffset ahora)
    {
        var entrega = new Entrega
        {
            Id = Guid.NewGuid(),
            ActividadId = actividadId,
            EstudianteId = estudianteId,
        };
        entrega.ReemplazarArchivos(archivos, ahora);
        return entrega;
    }

    public bool PerteneceA(Guid estudianteId) => EstudianteId == estudianteId;

    public ArchivoEntrega? Archivo(Guid archivoId) => _archivos.Find(a => a.Id == archivoId);

    /// <summary>
    /// Reemplaza todos los archivos y deja la entrega como ENVIADA.
    /// Devuelve las rutas de los blobs que dejaron de usarse.
    /// </summary>
    public IReadOnlyList<string> ReemplazarArchivos(IReadOnlyCollection<ArchivoGuardado> archivos, DateTimeOffset ahora)
    {
        if (archivos.Count == 0) throw new ArchivoInvalidoException("La entrega necesita al menos un archivo.");

        var anteriores = _archivos.Select(a => a.RutaBlob).ToList();
        _archivos.Clear();
        _archivos.AddRange(archivos.Select((a, i) => ArchivoEntrega.Crear(Id, i, a)));
        FechaEnvio = ahora.UtcDateTime;
        Estado = EstadoEntrega.ENVIADA;
        return anteriores;
    }

    public void Anular() => Estado = EstadoEntrega.ANULADA;
}

/// <summary>Un archivo de la entrega. El nombre original solo es metadato: el blob usa una ruta basada en GUID.</summary>
public sealed class ArchivoEntrega
{
    public Guid Id { get; private set; }
    public Guid EntregaId { get; private set; }
    /// <summary>Posición dentro de la entrega (0, 1, 2…).</summary>
    public int Orden { get; private set; }
    public string NombreArchivo { get; private set; } = "";
    public long Tamano { get; private set; }
    public string ContentType { get; private set; } = "";
    public string RutaBlob { get; private set; } = "";

    private ArchivoEntrega() { }

    internal static ArchivoEntrega Crear(Guid entregaId, int orden, ArchivoGuardado a) => new()
    {
        Id = Guid.NewGuid(),
        EntregaId = entregaId,
        Orden = orden,
        NombreArchivo = a.NombreOriginal,
        Tamano = a.Tamano,
        ContentType = a.ContentType,
        RutaBlob = a.RutaBlob,
    };
}

/// <summary>Metadatos de un archivo ya guardado en el almacén.</summary>
public sealed record ArchivoGuardado(string NombreOriginal, long Tamano, string ContentType, string RutaBlob);
