namespace Evaluaciones.Application.Abstracciones;

public interface IUnidadDeTrabajo
{
    /// <summary>Guarda todo en una sola transacción. Lanza ConflictoConcurrenciaException si otro cambio se adelantó.</summary>
    Task GuardarAsync(CancellationToken ct);
}
