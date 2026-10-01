namespace Entregas.Domain;

/// <summary>Reglas para aceptar una entrega nueva sobre una actividad (RN-04, RN-05).</summary>
public static class PoliticaSubida
{
    public static void Verificar(bool requiereEntrega, PlazoEntrega plazo, DateTimeOffset ahora)
    {
        if (!requiereEntrega) throw new ActividadSinEntregaException();
        plazo.ExigirVigente(ahora);
    }
}
