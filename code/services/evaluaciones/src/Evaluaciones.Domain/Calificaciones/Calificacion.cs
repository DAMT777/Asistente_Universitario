using Evaluaciones.Domain.Cursos;

namespace Evaluaciones.Domain.Calificaciones;

/// <summary>
/// Nota de un estudiante en una actividad. Cubre CU-05 (calificar una entrega), CU-06 (nota de una actividad
/// sin entrega), CU-07 (modificar una nota) y, con <see cref="Publicar"/>, CU-08.
/// </summary>
public sealed class Calificacion
{
    public const decimal NotaMinima = 0.0m;
    public const decimal NotaMaxima = 5.0m;
    /// <summary>La nota se digita con un decimal (3.5), igual que valida el frontend.</summary>
    public const int DecimalesNota = 1;
    public const int LongitudMaximaRetroalimentacion = 2000;

    private Calificacion() { }

    public Guid Id { get; private set; }
    public Guid ActividadId { get; private set; }
    /// <summary>Referencia al servicio de usuarios, sin llave foránea.</summary>
    public Guid EstudianteId { get; private set; }
    /// <summary>Referencia al servicio de entregas, sin llave foránea. Nula en parciales y sustentaciones.</summary>
    public Guid? EntregaId { get; private set; }
    /// <summary>Un 0.0 es una nota real; la ausencia de nota es la ausencia de la fila (RN-08).</summary>
    public decimal Valor { get; private set; }
    public string? Retroalimentacion { get; private set; }
    public EstadoCalificacion Estado { get; private set; }
    /// <summary>Token de concurrencia (ROWVERSION en SQL Server).</summary>
    public byte[] Version { get; set; } = [];

    /// <summary>Crea la calificación. Siempre nace como borrador.</summary>
    public static Calificacion Crear(Guid id, Actividad actividad, Guid estudianteId, Guid? entregaId, decimal valor, string? retroalimentacion)
    {
        ValidarValor(valor);
        var retro = NormalizarRetroalimentacion(retroalimentacion);
        ValidarEntrega(actividad, entregaId);

        return new Calificacion
        {
            Id = id,
            ActividadId = actividad.Id,
            EstudianteId = estudianteId,
            EntregaId = entregaId,
            Valor = valor,
            Retroalimentacion = retro,
            Estado = EstadoCalificacion.Borrador,
        };
    }

    /// <summary>
    /// Modifica la nota (CU-07). Si algo cambia, la calificación vuelve a borrador hasta que el profesor la publique
    /// de nuevo. Si no cambia nada, queda igual. Un <paramref name="entregaId"/> nulo conserva el vínculo actual.
    /// Devuelve true si hubo cambios.
    /// </summary>
    public bool Modificar(Actividad actividad, Guid? entregaId, decimal valor, string? retroalimentacion)
    {
        ValidarValor(valor);
        var retro = NormalizarRetroalimentacion(retroalimentacion);
        ValidarEntrega(actividad, entregaId);

        var nuevaEntrega = entregaId ?? EntregaId;
        if (valor == Valor && retro == Retroalimentacion && nuevaEntrega == EntregaId)
            return false;

        Valor = valor;
        Retroalimentacion = retro;
        EntregaId = nuevaEntrega;
        Estado = EstadoCalificacion.Borrador;
        return true;
    }

    /// <summary>Pasa de borrador a publicada (CU-08). Devuelve false si ya estaba publicada.</summary>
    public bool Publicar()
    {
        if (Estado == EstadoCalificacion.Publicada) return false;
        Estado = EstadoCalificacion.Publicada;
        return true;
    }

    private static void ValidarValor(decimal valor)
    {
        if (valor < NotaMinima || valor > NotaMaxima)
            throw new NotaFueraDeRangoException(valor);
        if (decimal.Round(valor, DecimalesNota) != valor)
            throw new ValidacionFallidaException($"La nota admite un solo decimal, por ejemplo 3.5 (recibido {valor}).");
    }

    private static string? NormalizarRetroalimentacion(string? retro)
    {
        var limpia = retro?.Trim();
        if (string.IsNullOrEmpty(limpia)) return null;
        if (limpia.Length > LongitudMaximaRetroalimentacion)
            throw new ValidacionFallidaException($"La retroalimentación admite máximo {LongitudMaximaRetroalimentacion} caracteres.");
        return limpia;
    }

    private static void ValidarEntrega(Actividad actividad, Guid? entregaId)
    {
        if (entregaId is not null && !actividad.RequiereEntrega)
            throw new ValidacionFallidaException("La actividad no requiere entrega, así que la nota no puede asociarse a una entrega.");
    }
}
