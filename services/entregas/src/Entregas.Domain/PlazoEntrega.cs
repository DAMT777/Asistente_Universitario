namespace Entregas.Domain;

/// <summary>
/// RN-05 / RN-06: se acepta hasta la fecha límite inclusive. La comparación es siempre en UTC.
/// </summary>
public readonly record struct PlazoEntrega(DateTimeOffset FechaLimite)
{
    public bool EstaVigente(DateTimeOffset ahora) => ahora.ToUniversalTime() <= FechaLimite.ToUniversalTime();

    public void ExigirVigente(DateTimeOffset ahora)
    {
        if (!EstaVigente(ahora)) throw new FechaLimiteVencidaException();
    }
}
