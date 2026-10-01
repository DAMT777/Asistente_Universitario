namespace Unillanos.Pruebas.Compartidas;

/// <summary>TimeProvider cuyo "ahora" fija la prueba.</summary>
public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public DateTimeOffset Ahora { get; set; } = ahora;

    public override DateTimeOffset GetUtcNow() => Ahora.ToUniversalTime();

    /// <summary>"Ahora" real truncado a segundos: los JWT de prueba siguen siendo válidos.</summary>
    public static DateTimeOffset AhoraTruncado()
    {
        var ahora = DateTimeOffset.UtcNow;
        return ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));
    }
}
