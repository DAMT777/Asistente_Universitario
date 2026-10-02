namespace Entregas.Domain;

/// <summary>
/// RN-05 / RN-06: se acepta hasta la fecha límite inclusive. La comparación es siempre en UTC.
/// Una actividad sin fecha límite (null) no vence.
/// </summary>
public readonly record struct PlazoEntrega(DateTimeOffset? FechaLimite)
{
    public bool EstaVigente(DateTimeOffset ahora) => FechaLimite is not { } limite || ahora.ToUniversalTime() <= limite.ToUniversalTime();

    public void ExigirVigente(DateTimeOffset ahora)
    {
        if (!EstaVigente(ahora)) throw new FechaLimiteVencidaException();
    }
}
