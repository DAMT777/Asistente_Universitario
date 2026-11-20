import { describe, expect, it, vi } from 'vitest';
import { createHttpApi } from './http';
import { createMockApi } from './mock/mockApi';

const docente = { usuario: 'lrincon', contrasena: 'demo123' };
const diego = { usuario: '160005017', contrasena: 'demo123' };
const valentina = { usuario: '160005021', contrasena: 'demo123' };
const nuevo = () => createMockApi({ hoy: () => '2026-10-01T17:00:00Z', latenciaMs: 0 });
function respuesta(valor: unknown, status = 200) { return new Response(JSON.stringify(valor), { status, headers: { 'Content-Type': 'application/json' } }); }

describe('notificaciones por HTTP (contracts/evaluaciones.yaml)', () => {
  it('adapta la bandeja del servicio y marca leídas por id o todas', async () => {
    const fetchImpl = vi.fn()
      .mockResolvedValueOnce(respuesta({ noLeidas: 1, notificaciones: [{ id: 'n1', tipo: 'NOTA_PUBLICADA', titulo: 'Nota publicada: Taller 1', mensaje: 'Tu nota es 4.0.', cursoId: 'c1', actividadId: 'a1', fechaCreacion: '2026-11-19T23:00:00Z', leida: false }] }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(respuesta({ marcadas: 3 }));
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'token', fetchImpl });

    expect(await api.notificaciones.listar()).toEqual({ noLeidas: 1, notificaciones: [{ id: 'n1', tipo: 'nota_publicada', titulo: 'Nota publicada: Taller 1', mensaje: 'Tu nota es 4.0.', cursoId: 'c1', actividadId: 'a1', fecha: '2026-11-19T23:00:00Z', leida: false }] });
    expect(fetchImpl.mock.calls[0][0]).toBe('/api/notificaciones');
    await api.notificaciones.marcarLeida('n1');
    expect(fetchImpl.mock.calls[1]).toMatchObject(['/api/notificaciones/n1/leida', { method: 'POST' }]);
    await api.notificaciones.marcarTodasLeidas();
    expect(fetchImpl.mock.calls[2]).toMatchObject(['/api/notificaciones/leidas', { method: 'POST' }]);
  });
});

describe('notificaciones en la api simulada', () => {
  it('una actividad nueva llega a los estudiantes y no al docente', async () => {
    const api = nuevo(); await api.auth.login(docente);
    const s5 = (await api.actividades.listar('sim')).find(x => x.id === 's5')!;
    await api.actividades.editar('s5', { ...s5, peso: 80 }); // deja espacio en el corte 3
    const a = await api.actividades.crear({ cursoId: 'sim', corte: 3, titulo: 'Quiz sorpresa', peso: 20, vence: '2026-10-20T23:59:00-05:00', requiereEntrega: true });
    expect((await api.notificaciones.listar()).notificaciones).toEqual([]);

    await api.auth.login(diego);
    const bandeja = await api.notificaciones.listar();
    expect(bandeja.notificaciones[0]).toMatchObject({ tipo: 'nueva_actividad', titulo: 'Nueva actividad: Quiz sorpresa', cursoId: 'sim', actividadId: a.id, leida: false });
    expect(bandeja.notificaciones[0].mensaje).toContain('20/10/2026 23:59');
  });

  it('calificar en borrador no avisa; publicar sí, solo al estudiante y con su nota', async () => {
    const api = nuevo(); await api.auth.login(diego);
    const antes = (await api.notificaciones.listar()).noLeidas;

    await api.auth.login(docente);
    await api.calificaciones.guardar({ actividadId: 's3', estudianteId: 'e1', nota: 4.5, retro: 'Muy bien', publicar: false });
    await api.auth.login(diego);
    expect((await api.notificaciones.listar()).noLeidas).toBe(antes);

    await api.auth.login(docente);
    await api.calificaciones.publicarBorradores('s3');
    await api.auth.login(diego);
    const b = await api.notificaciones.listar();
    expect(b.noLeidas).toBe(antes + 1);
    expect(b.notificaciones[0]).toMatchObject({ tipo: 'nota_publicada', actividadId: 's3' });
    expect(b.notificaciones[0].mensaje).toContain('4.5');

    await api.auth.login(valentina); // también tenía un borrador en s3 (4.2)
    expect((await api.notificaciones.listar()).notificaciones[0].mensaje).toContain('4.2');
  });

  it('cambiar una actividad avisa qué cambió y guardarla igual no avisa', async () => {
    const api = nuevo(); await api.auth.login(docente);
    const s5 = (await api.actividades.listar('sim')).find(a => a.id === 's5')!;
    await api.actividades.editar('s5', { ...s5 });
    await api.auth.login(diego);
    const sinCambios = (await api.notificaciones.listar()).notificaciones.length;

    await api.auth.login(docente);
    await api.actividades.editar('s5', { ...s5, titulo: 'Proyecto integrador' });
    await api.auth.login(diego);
    const b = await api.notificaciones.listar();
    expect(b.notificaciones.length).toBe(sinCambios + 1);
    expect(b.notificaciones[0]).toMatchObject({ tipo: 'curso_actualizado', titulo: 'Cambios en Proyecto integrador' });
  });

  it('marca leída solo la propia y marcar todas deja el contador en cero', async () => {
    const api = nuevo(); await api.auth.login(diego);
    const b = await api.notificaciones.listar();
    expect(b.noLeidas).toBeGreaterThan(0);
    await api.notificaciones.marcarLeida(b.notificaciones[0].id);
    expect((await api.notificaciones.listar()).noLeidas).toBe(b.noLeidas - 1);

    await api.auth.login(valentina);
    await expect(api.notificaciones.marcarLeida(b.notificaciones[1].id)).rejects.toThrow('no existe');

    await api.auth.login(diego);
    await api.notificaciones.marcarTodasLeidas();
    expect((await api.notificaciones.listar()).noLeidas).toBe(0);
  });

  it('el docente no tiene avisos: la bandeja llega vacía', async () => {
    const api = nuevo(); await api.auth.login(docente);
    expect(await api.notificaciones.listar()).toEqual({ noLeidas: 0, notificaciones: [] });
  });
});
