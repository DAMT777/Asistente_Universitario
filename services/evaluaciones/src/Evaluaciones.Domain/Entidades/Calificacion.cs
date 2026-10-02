using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Domain.Entidades;

/// <summary>
/// Nota de un estudiante en una actividad. Si no existe la fila, la actividad está sin calificar,
/// y eso es distinto de una calificación con valor 0 (RN-08).
/// </summary>
public class Calificacion
{
    public const decimal NotaMinima = 0.0m;
    public const decimal NotaMaxima = 5.0m;
    public const int LargoMaximoRetroalimentacion = 2000;

    public Guid Id { get; set; }
    public Guid ActividadId { get; set; }
    public Guid EstudianteId { get; set; }
    public Guid? EntregaId { get; set; }
    public decimal Valor { get; set; }
    public string? Retroalimentacion { get; set; }
    public EstadoCalificacion Estado { get; set; }
    public byte[]? Version { get; set; }

    /// <summary>CU-05 y CU-06. Toda calificación nueva nace en borrador (RN-08).</summary>
    public static Calificacion Nueva(Guid actividadId, Guid estudianteId, decimal valor, string retroalimentacion, Guid? entregaId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ActividadId = actividadId,
            EstudianteId = estudianteId,
            EntregaId = entregaId,
            Valor = valor,
            Retroalimentacion = retroalimentacion,
            Estado = EstadoCalificacion.Borrador
        };

    /// <summary>
    /// CU-07. Cambia la nota o la retroalimentación. Si algo cambió, vuelve a borrador hasta que el profesor
    /// publique de nuevo la actividad (CU-08). Si no cambió nada, conserva su estado.
    /// </summary>
    /// <returns>Verdadero si hubo cambios.</returns>
    public bool Modificar(decimal valor, string retroalimentacion, Guid? entregaId)
    {
        var nuevaEntrega = entregaId ?? EntregaId;
        if (Valor == valor && (Retroalimentacion ?? "") == retroalimentacion && EntregaId == nuevaEntrega)
            return false;

        Valor = valor;
        Retroalimentacion = retroalimentacion;
        EntregaId = nuevaEntrega;
        Estado = EstadoCalificacion.Borrador;
        return true;
    }

    /// <summary>CU-08. La calificación pasa a ser visible para el estudiante (RN-09).</summary>
    /// <returns>Verdadero si estaba en borrador.</returns>
    public bool Publicar()
    {
        if (Estado == EstadoCalificacion.Publicada) return false;
        Estado = EstadoCalificacion.Publicada;
        return true;
    }

    /// <summary>RN-03. Escala de 0.0 a 5.0 con un decimal como máximo.</summary>
    public static decimal ValidarValor(decimal? valor)
    {
        if (valor is null)
            throw new DominioException(CodigosError.ValidacionFallida, "Falta la nota (campo valor).");

        if (valor < NotaMinima || valor > NotaMaxima)
            throw new DominioException(CodigosError.NotaFueraDeRango, "La nota debe estar entre 0.0 y 5.0.");

        if (decimal.Round(valor.Value, 1) != valor.Value)
            throw new DominioException(CodigosError.ValidacionFallida, "La nota admite como máximo un decimal, por ejemplo 3.5.");

        return valor.Value;
    }

    /// <summary>La retroalimentación es opcional. Se guarda sin espacios sobrantes y nunca nula.</summary>
    public static string ValidarRetroalimentacion(string? texto)
    {
        var limpio = (texto ?? "").Trim();
        if (limpio.Length > LargoMaximoRetroalimentacion)
            throw new DominioException(
                CodigosError.ValidacionFallida,
                $"La retroalimentación admite como máximo {LargoMaximoRetroalimentacion} caracteres.");
        return limpio;
    }
}
