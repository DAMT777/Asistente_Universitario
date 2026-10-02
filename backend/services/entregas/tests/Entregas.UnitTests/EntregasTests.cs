using Entregas.Application.CasosDeUso;
using Entregas.Domain;
using Entregas.UnitTests.Apoyo;

namespace Entregas.UnitTests;

/// <summary>CU-04 (el profesor ve las entregas), entregas propias y descarga del archivo.</summary>
public class EntregasTests
{
    // ───────── CU-04 ─────────

    [Fact]
    public async Task CU04_el_profesor_ve_las_entregas_de_su_actividad_la_mas_reciente_primero()
    {
        var e = new Escenario();

        var lista = await new ListarEntregasDeActividad(e, e).EjecutarAsync(Escenario.Profesor, Escenario.Taller1, default);

        Assert.Equal(new[] { Escenario.Luis, Escenario.Ana }, lista.Select(x => x.EstudianteId));
        Assert.All(lista, x => Assert.Equal("ENVIADA", x.Estado));
        Assert.All(lista, x => Assert.Equal(Escenario.Taller1, x.ActividadId));
        Assert.Equal("taller1-luis.pdf", lista[0].NombreArchivo);
    }

    [Fact]
    public async Task CU04_incluye_las_entregas_anuladas_con_su_estado()
    {
        var e = new Escenario();
        e.De(Escenario.Taller1, Escenario.Ana).Estado = EstadoEntrega.Anulada;

        var lista = await new ListarEntregasDeActividad(e, e).EjecutarAsync(Escenario.Profesor, Escenario.Taller1, default);

        Assert.Equal("ANULADA", lista.Single(x => x.EstudianteId == Escenario.Ana).Estado);
    }

    [Fact]
    public async Task CU04_una_actividad_sin_entregas_devuelve_lista_vacia()
    {
        var e = new Escenario();
        var parcial = Guid.NewGuid();
        e.Actividades[parcial] = new(parcial, Escenario.Curso, Escenario.Profesor, null, false, null);

        var lista = await new ListarEntregasDeActividad(e, e).EjecutarAsync(Escenario.Profesor, parcial, default);

        Assert.Empty(lista);
    }

    [Fact]
    public async Task CU04_otro_profesor_recibe_SIN_PERMISO()
    {
        // E-18 y RN-15: verificar solo el rol no es suficiente.
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => new ListarEntregasDeActividad(e, e).EjecutarAsync(Escenario.OtroProfesor, Escenario.Taller1, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task CU04_una_actividad_inexistente_responde_NO_ENCONTRADO()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => new ListarEntregasDeActividad(e, e).EjecutarAsync(Escenario.Profesor, Guid.NewGuid(), default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public async Task CU04_si_evaluaciones_no_responde_no_asume_nada_y_responde_SERVICIO_NO_DISPONIBLE()
    {
        var e = new Escenario { EvaluacionesCaido = true };

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => new ListarEntregasDeActividad(e, e).EjecutarAsync(Escenario.Profesor, Escenario.Taller1, default));

        Assert.Equal(CodigosError.ServicioNoDisponible, ex.Codigo);
    }

    // ───────── Entregas propias ─────────

    [Fact]
    public async Task El_estudiante_solo_ve_sus_entregas_y_puede_filtrar_por_actividad()
    {
        var e = new Escenario();
        var mias = new ListarMisEntregas(e);

        var todas = await mias.EjecutarAsync(Escenario.Ana, null, default);
        var delTaller = await mias.EjecutarAsync(Escenario.Ana, Escenario.Taller1, default);

        Assert.Equal(2, todas.Count);
        Assert.All(todas, x => Assert.Equal(Escenario.Ana, x.EstudianteId));
        Assert.Equal(Escenario.Taller1, Assert.Single(delTaller).ActividadId);
        Assert.Equal(0, e.ConsultasAEvaluaciones); // no necesita a Evaluaciones
    }

    // ───────── Descarga ─────────

    [Fact]
    public async Task El_profesor_dueno_descarga_el_archivo()
    {
        var e = new Escenario();
        var entrega = e.De(Escenario.Taller1, Escenario.Ana);

        var archivo = await new DescargarArchivo(e, e, e).EjecutarAsync(Escenario.Profesor, Roles.Profesor, entrega.Id, default);

        Assert.Equal("taller1-ana.pdf", archivo.NombreArchivo);
        Assert.Equal("application/pdf", archivo.TipoContenido);
        Assert.Equal("contenido de taller1-ana.pdf", new StreamReader(archivo.Contenido).ReadToEnd());
    }

    [Fact]
    public async Task El_estudiante_autor_descarga_y_otro_estudiante_no_la_ve()
    {
        var e = new Escenario();
        var deAna = e.De(Escenario.Taller1, Escenario.Ana);
        var descargar = new DescargarArchivo(e, e, e);

        var propia = await descargar.EjecutarAsync(Escenario.Ana, Roles.Estudiante, deAna.Id, default);
        var ex = await Assert.ThrowsAsync<DominioException>(
            () => descargar.EjecutarAsync(Escenario.Luis, Roles.Estudiante, deAna.Id, default));

        Assert.Equal("taller1-ana.pdf", propia.NombreArchivo);
        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo); // RN-16: no revela que existe
    }

    [Fact]
    public async Task Otro_profesor_no_descarga()
    {
        var e = new Escenario();

        var ex = await Assert.ThrowsAsync<DominioException>(() => new DescargarArchivo(e, e, e)
            .EjecutarAsync(Escenario.OtroProfesor, Roles.Profesor, e.De(Escenario.Taller1, Escenario.Ana).Id, default));

        Assert.Equal(CodigosError.SinPermiso, ex.Codigo);
    }

    [Fact]
    public async Task Una_entrega_anulada_no_se_descarga()
    {
        var e = new Escenario();
        var entrega = e.De(Escenario.Taller1, Escenario.Ana);
        entrega.Estado = EstadoEntrega.Anulada;

        var ex = await Assert.ThrowsAsync<DominioException>(
            () => new DescargarArchivo(e, e, e).EjecutarAsync(Escenario.Profesor, Roles.Profesor, entrega.Id, default));

        Assert.Equal(CodigosError.NoEncontrado, ex.Codigo);
    }

    [Fact]
    public void La_ruta_del_archivo_usa_un_guid_y_no_permite_salir_de_la_carpeta()
    {
        var ruta = Entrega.RutaPara(Escenario.Taller1, Escenario.Ana, "../../secreto.pdf");

        Assert.StartsWith($"{Escenario.Taller1}/{Escenario.Ana}/", ruta);
        Assert.EndsWith("-secreto.pdf", ruta);
        Assert.DoesNotContain("..", ruta);
    }
}
