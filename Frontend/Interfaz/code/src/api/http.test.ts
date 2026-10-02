import { describe, expect, it, vi } from 'vitest';
import { createHttpApi } from './http';

function respuesta(valor: unknown, status = 200) { return new Response(JSON.stringify(valor), { status, headers: { 'Content-Type': 'application/json' } }); }
describe('adaptador de los contratos de la guía técnica', () => {
  it('adapta login y usa el rol devuelto por el servicio', async () => {
    const fetchImpl = vi.fn().mockResolvedValue(respuesta({ accessToken: 'token', usuario: { id: 'p1', nombre: 'Profesora', rol: 'PROFESOR' } }));
    const api = createHttpApi({ baseUrl: '/api', getToken: () => null, fetchImpl });
    const sesion = await api.auth.login({ usuario: 'P0001', contrasena: 'Demo1234!', rol: 'estudiante' });
    expect(sesion.usuario.rol).toBe('docente');
    expect(JSON.parse(fetchImpl.mock.calls[0][1].body)).toEqual({ usuario: 'P0001', password: 'Demo1234!' });
  });
  it('convierte la matriz del estudiante sin confundir cero con sin publicar', async () => {
    const fetchImpl = vi.fn().mockResolvedValueOnce(respuesta({ id: 'e1', nombre: 'Ana', rol: 'ESTUDIANTE' })).mockResolvedValueOnce(respuesta({ cursos: [{ cursoId: 'c1', cortes: [{ corte: 1, nota: 0, publicado: true }, { corte: 2, nota: null, publicado: false }] }] }));
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'token', fetchImpl });
    expect(await api.cortes.listar()).toEqual([{ cursoId: 'c1', estudianteId: 'e1', corte: 1, nota: 0, fechaPublicacion: '' }]);
    expect(fetchImpl.mock.calls[1][0]).toBe('/api/mis-notas');
  });
  it('publica únicamente al estudiante solicitado y muestra los rechazos individuales', async () => {
    const fetchImpl = vi.fn().mockResolvedValueOnce(respuesta({ publicados: [{ estudianteId: 'e1', nota: 3.2 }], rechazados: [] })).mockResolvedValueOnce(respuesta({ publicados: [], rechazados: [{ estudianteId: 'e1', mensaje: 'Tiene 1 calificación en borrador.' }] }));
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'token', fetchImpl });
    const input = { cursoId: 'c1', estudianteId: 'e1', corte: 1 as const, corregir: false, omitirBorradores: false };
    expect((await api.cortes.publicar(input)).nota).toBe(3.2);
    expect(JSON.parse(fetchImpl.mock.calls[0][1].body)).toEqual({ estudiantes: ['e1'], omitirBorradores: false });
    await expect(api.cortes.publicar(input)).rejects.toThrow('borrador');
  });
  it('anula por identificador de entrega y conserva el token en la solicitud', async () => {
    const fetchImpl = vi.fn().mockResolvedValueOnce(respuesta([{ id: 'ent1', actividadId: 'a1', estudianteId: 'e1', fechaEnvio: '2026-10-01T10:00:00Z', estado: 'ENVIADA', nombreArchivo: 'x.pdf', tamano: 20 }])).mockResolvedValueOnce(new Response(null, { status: 204 }));
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'token', fetchImpl });
    await api.calificaciones.anularEntrega('a1');
    expect(fetchImpl.mock.calls[1][0]).toBe('/api/entregas/ent1');
    expect(fetchImpl.mock.calls[1][1]).toMatchObject({ method: 'DELETE', headers: { Authorization: 'Bearer token' } });
  });
});
