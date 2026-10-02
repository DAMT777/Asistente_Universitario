using Evaluaciones.Application.Abstracciones;
using Evaluaciones.Domain;
using Evaluaciones.Infrastructure.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Evaluaciones.Infrastructure.Repositorios;

internal sealed class UnidadDeTrabajo(EvaluacionesDbContext db) : IUnidadDeTrabajo
{
    public async Task GuardarAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Otra modificación cambió la fila (ROWVERSION distinto) entre la lectura y el guardado.
            throw new ConflictoConcurrenciaException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Dos altas simultáneas de la misma (actividad, estudiante): la segunda choca con el índice único.
            throw new ConflictoConcurrenciaException("Esta actividad ya fue calificada para el estudiante por otra solicitud. Consulta la calificación y modifícala.");
        }
    }
}
