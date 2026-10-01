using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Unillanos.Evaluaciones.Infrastructure.Persistencia.Configuraciones;

/// <summary>DATETIME2 no guarda la zona: se escribe en UTC y se lee marcado como UTC.</summary>
internal static class Utc
{
    public static readonly ValueConverter<DateTime, DateTime> Convertidor = new(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
