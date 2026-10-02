using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Evaluaciones.Infrastructure.Persistencia;

/// <summary>
/// datetime2 no guarda la zona horaria. Todas las fechas del sistema están en UTC (DA-10), así que al leerlas
/// se marcan como UTC para que el JSON salga con "Z" como pide el contrato.
/// </summary>
internal static class EnUtc
{
    public static readonly ValueConverter<DateTime, DateTime> Valor =
        new(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    public static readonly ValueConverter<DateTime?, DateTime?> Nulable =
        new(v => v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
}
