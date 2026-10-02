using Microsoft.Extensions.Logging.Abstractions;
using Entregas.Application.Comun;
using Entregas.Application.Puertos;
using Entregas.Domain;

namespace Entregas.UnitTests;

public sealed class PersistenciaConCompensacionTests
{
    [Fact]
    public async Task Si_la_base_falla_elimina_el_blob_nuevo_y_conserva_el_anterior()
    {
        var almacen = new AlmacenRegistro();
        var persistencia = new PersistenciaConCompensacion(new RepositorioQueFalla(), almacen, NullLogger<PersistenciaConCompensacion>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => persistencia.GuardarAsync(["nuevo1", "nuevo2"], ["anterior"], CancellationToken.None));

        Assert.Equal(["nuevo1", "nuevo2"], almacen.Eliminados);
    }

    [Fact]
    public async Task Si_la_base_confirma_elimina_el_blob_reemplazado()
    {
        var almacen = new AlmacenRegistro();
        var persistencia = new PersistenciaConCompensacion(new RepositorioQueConfirma(), almacen, NullLogger<PersistenciaConCompensacion>.Instance);

        await persistencia.GuardarAsync(["nuevo"], ["anterior1", "anterior2"], CancellationToken.None);

        Assert.Equal(["anterior1", "anterior2"], almacen.Eliminados);
    }

    private sealed class AlmacenRegistro : IAlmacenArchivos
    {
        public List<string> Eliminados { get; } = [];
        public Task GuardarAsync(string ruta, Stream contenido, string contentType, CancellationToken ct) => Task.CompletedTask;
        public Task<Stream?> AbrirAsync(string ruta, CancellationToken ct) => Task.FromResult<Stream?>(null);
        public Task EliminarAsync(string ruta, CancellationToken ct)
        {
            Eliminados.Add(ruta);
            return Task.CompletedTask;
        }
    }

    private class RepositorioQueConfirma : IEntregaRepositorio
    {
        public Task<Entrega?> ObtenerAsync(Guid entregaId, CancellationToken ct) => Task.FromResult<Entrega?>(null);
        public Task<Entrega?> ObtenerPorActividadYEstudianteAsync(Guid actividadId, Guid estudianteId, CancellationToken ct) => Task.FromResult<Entrega?>(null);
        public Task<IReadOnlyList<Entrega>> ListarDelEstudianteAsync(Guid estudianteId, Guid? actividadId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Entrega>>([]);
        public Task<IReadOnlyList<Entrega>> ListarDeActividadAsync(Guid actividadId, CancellationToken ct) => Task.FromResult<IReadOnlyList<Entrega>>([]);
        public void Agregar(Entrega entrega) { }
        public virtual Task GuardarCambiosAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class RepositorioQueFalla : RepositorioQueConfirma
    {
        public override Task GuardarCambiosAsync(CancellationToken ct) => throw new InvalidOperationException("BD caída");
    }
}
