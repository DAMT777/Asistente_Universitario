using Evaluaciones.Domain.Entidades;
using Evaluaciones.Domain.Errores;

namespace Evaluaciones.Domain.Ponderado;

/// <summary>
/// Cálculo del ponderado (RN-10, RN-11, anexo B). Es código puro: recibe notas y pesos y devuelve
/// resultados, sin acceso a base de datos. El ponderado nunca se guarda, solo la PublicacionCorte.
/// </summary>
public static class MotorPonderado
{
    public const int CantidadCortes = 3;

    public static bool EsCorteValido(int corte) => corte is >= 1 and <= CantidadCortes;

    /// <summary>Un decimal, mitad hacia arriba (DA-01).</summary>
    public static decimal Redondear(decimal valor) => Math.Round(valor, 1, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Nota del corte = suma de valor × peso / 100 de las actividades calificadas.
    /// Las actividades sin calificación no aportan y no se tratan como 0. No se normaliza (DA-02).
    /// </summary>
    public static decimal NotaCorte(
        IEnumerable<Actividad> actividadesDelCorte,
        IReadOnlyDictionary<Guid, decimal> valorPorActividad)
    {
        decimal suma = 0;
        foreach (var actividad in actividadesDelCorte)
        {
            if (valorPorActividad.TryGetValue(actividad.Id, out var valor))
                suma += valor * actividad.Peso / 100m;
        }
        return suma;
    }

    /// <summary>
    /// Definitiva parcial = suma de nota del corte × peso del corte / 100 sobre los cortes recibidos.
    /// Sin cortes devuelve 0.0.
    /// </summary>
    public static decimal DefinitivaParcial(Curso curso, IReadOnlyDictionary<int, decimal> notaPorCorte)
    {
        decimal total = 0;
        foreach (var (corte, nota) in notaPorCorte)
            total += nota * curso.PesoDeCorte(corte) / 100m;
        return Redondear(total);
    }

    /// <summary>
    /// Nota de corte que se puede publicar para un estudiante (RN-12). Usa solo las calificaciones
    /// publicadas. Si hay borradores y no se pidió omitirlos, o si no hay ninguna publicada, lanza
    /// <see cref="DominioException"/>.
    /// </summary>
    public static decimal NotaCortePublicable(
        IReadOnlyCollection<Actividad> actividadesDelCorte,
        IEnumerable<Calificacion> calificacionesDelEstudiante,
        bool omitirBorradores)
    {
        var idsDelCorte = actividadesDelCorte.Select(a => a.Id).ToHashSet();
        var delCorte = calificacionesDelEstudiante.Where(c => idsDelCorte.Contains(c.ActividadId)).ToList();

        var borradores = delCorte.Count(c => c.Estado == EstadoCalificacion.Borrador);
        if (borradores > 0 && !omitirBorradores)
        {
            var texto = borradores == 1 ? "1 calificación" : $"{borradores} calificaciones";
            throw new DominioException(CodigosError.CalificacionesEnBorrador, $"Tiene {texto} en borrador.");
        }

        var publicadas = delCorte
            .Where(c => c.Estado == EstadoCalificacion.Publicada)
            .ToDictionary(c => c.ActividadId, c => c.Valor);
        if (publicadas.Count == 0)
            throw new DominioException(
                CodigosError.SinCalificaciones,
                "El estudiante no tiene calificaciones publicadas en el corte.");

        return Redondear(NotaCorte(actividadesDelCorte, publicadas));
    }
}
