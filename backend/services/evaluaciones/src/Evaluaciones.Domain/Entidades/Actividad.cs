using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Domain.Entidades;

public class Actividad
{
    public const int LargoMaximoTitulo = 150;

    public Guid Id { get; set; }
    public Guid CursoId { get; set; }
    public string Titulo { get; set; } = "";
    public int Corte { get; set; }

    /// <summary>Porcentaje de 0 a 100 dentro de su corte (DA-03).</summary>
    public decimal Peso { get; set; }

    /// <summary>En UTC (DA-10).</summary>
    public DateTime? FechaLimite { get; set; }

    public bool RequiereEntrega { get; set; }

    /// <summary>CU-03. Crea una actividad con los datos ya validados por <see cref="ValidarDatos"/>.</summary>
    public static Actividad Nueva(Guid cursoId, DatosActividad datos) => new()
    {
        Id = Guid.NewGuid(),
        CursoId = cursoId,
        Titulo = datos.Titulo,
        Corte = datos.Corte,
        Peso = datos.Peso,
        FechaLimite = datos.FechaLimite,
        RequiereEntrega = datos.RequiereEntrega
    };

    /// <summary>CU-03. Edita la actividad. El curso no cambia.</summary>
    public void Editar(DatosActividad datos)
    {
        Titulo = datos.Titulo;
        Corte = datos.Corte;
        Peso = datos.Peso;
        FechaLimite = datos.FechaLimite;
        RequiereEntrega = datos.RequiereEntrega;
    }

    /// <summary>
    /// Reglas de una actividad que no dependen de las demás (RN-02, RN-04, RN-05): título obligatorio,
    /// corte 1 a 3, peso mayor que 0 y hasta 100, y fecha límite obligatoria si requiere entrega.
    /// </summary>
    public static DatosActividad ValidarDatos(
        string? titulo, int? corte, decimal? peso, DateTimeOffset? fechaLimite, bool? requiereEntrega)
    {
        var limpio = (titulo ?? "").Trim();
        if (limpio.Length == 0)
            throw new DominioException(CodigosError.ValidacionFallida, "El título es obligatorio.");
        if (limpio.Length > LargoMaximoTitulo)
            throw new DominioException(CodigosError.ValidacionFallida, $"El título admite como máximo {LargoMaximoTitulo} caracteres.");

        if (corte is not (>= 1 and <= 3))
            throw new DominioException(CodigosError.ValidacionFallida, "El corte debe ser 1, 2 o 3.");

        if (peso is null || peso <= 0 || peso > 100)
            throw new DominioException(CodigosError.ValidacionFallida, "El peso debe ser mayor que 0 y como máximo 100.");
        if (decimal.Round(peso.Value, 2) != peso.Value)
            throw new DominioException(CodigosError.ValidacionFallida, "El peso admite dos decimales como máximo.");

        var conEntrega = requiereEntrega ?? false;
        if (conEntrega && fechaLimite is null)
            throw new DominioException(
                CodigosError.ValidacionFallida, "Una actividad que requiere entrega necesita fecha límite.");

        return new DatosActividad(limpio, corte.Value, peso.Value, fechaLimite?.UtcDateTime, conEntrega);
    }

    /// <summary>
    /// RN-02. Los pesos de las actividades de un mismo corte no pueden sumar más de 100.
    /// <paramref name="otrasDelCorte"/> no debe incluir la actividad que se crea o edita.
    /// </summary>
    public static void ValidarPesoDisponible(int corte, decimal peso, IEnumerable<Actividad> otrasDelCorte)
    {
        var usado = otrasDelCorte.Sum(a => a.Peso);
        if (usado + peso > 100)
            throw new DominioException(
                CodigosError.PesosActividadExcedidos,
                $"Los pesos del corte {corte} sumarían {usado + peso:0.##}. Solo quedan {100 - usado:0.##}% disponibles.");
    }
}

/// <summary>Datos de una actividad ya validados y normalizados (título sin espacios sobrantes, fecha en UTC).</summary>
public sealed record DatosActividad(string Titulo, int Corte, decimal Peso, DateTime? FechaLimite, bool RequiereEntrega);
