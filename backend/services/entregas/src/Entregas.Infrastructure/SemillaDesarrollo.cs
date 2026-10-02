using System.Text;
using Entregas.Domain;
using Entregas.Infrastructure.Archivos;
using Entregas.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entregas.Infrastructure;

/// <summary>
/// Entregas de demostración (sección 9.8). Idempotente: si ya hay entregas no hace nada. Los identificadores
/// son fijos y coinciden con la semilla de Evaluaciones (Taller 1 y Proyecto 1; Ana y Luis).
/// </summary>
public static class SemillaDesarrollo
{
    public static readonly Guid Taller1 = new("d0000000-0000-0000-0000-000000000001");
    public static readonly Guid Proyecto1 = new("d0000000-0000-0000-0000-000000000003");
    public static readonly Guid Ana = new("b0000000-0000-0000-0000-000000000001");
    public static readonly Guid Luis = new("b0000000-0000-0000-0000-000000000002");

    public static async Task AplicarAsync(IServiceProvider servicios, TimeProvider reloj, CancellationToken ct = default)
    {
        using var alcance = servicios.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<EntregasDbContext>();
        var almacen = alcance.ServiceProvider.GetRequiredService<AlmacenArchivosLocal>();

        if (db.Database.IsInMemory())
            await db.Database.EnsureCreatedAsync(ct);

        if (await db.Entregas.AnyAsync(ct)) return;

        var ahora = reloj.GetUtcNow().UtcDateTime;
        ahora = new DateTime(ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        (Guid Id, Guid Actividad, Guid Estudiante, DateTime Fecha, string Nombre)[] datos =
        [
            (new("e0000000-0000-0000-0000-000000000001"), Taller1, Ana, ahora.AddDays(-1), "taller1-ana.txt"),
            (new("e0000000-0000-0000-0000-000000000002"), Taller1, Luis, ahora.AddHours(-2), "taller1-luis.txt"),
            (new("e0000000-0000-0000-0000-000000000003"), Proyecto1, Ana, ahora.AddDays(-4), "proyecto1-ana.txt"),
            (new("e0000000-0000-0000-0000-000000000004"), Proyecto1, Luis, ahora.AddDays(-5), "proyecto1-luis.txt"),
        ];

        foreach (var (id, actividad, estudiante, fecha, nombre) in datos)
        {
            var contenido = Encoding.UTF8.GetBytes(
                $"Entrega de demostración: {nombre}\nEnviada el {fecha:yyyy-MM-dd HH:mm} UTC.\n");
            var ruta = Entrega.RutaPara(actividad, estudiante, nombre);
            await almacen.GuardarAsync(ruta, contenido, ct);

            db.Entregas.Add(new Entrega
            {
                Id = id, ActividadId = actividad, EstudianteId = estudiante, FechaEnvio = fecha,
                Estado = EstadoEntrega.Enviada, NombreArchivo = nombre, Tamano = contenido.Length, RutaBlob = ruta
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
