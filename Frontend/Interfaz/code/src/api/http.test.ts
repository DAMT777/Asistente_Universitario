import { describe, expect, it } from 'vitest';
import { createHttpApi } from './http';
import { ApiError } from './contratos';

interface Llamada { url: string; init: RequestInit }

function fetchFalso(respuestas: Array<{ status: number; cuerpo?: unknown }>) {
  const llamadas: Llamada[] = [];
  const impl = (async (url: string, init: RequestInit) => {
    llamadas.push({ url, init });
    const r = respuestas.shift()!;
    return new Response(r.cuerpo === undefined ? null : JSON.stringify(r.cuerpo), { status: r.status });
  }) as unknown as typeof fetch;
  return { impl, llamadas };
}

const entrega = { id: 'x1', actividadId: 'd1', estudianteId: 'e1', fechaEnvio: '2026-10-01T15:00:00Z', estado: 'ENVIADA', nombreArchivo: 't.pdf', tamano: 10 };

describe('createHttpApi · estudiante', () => {
  it('pide la matriz con el token y valida la respuesta', async () => {
    const f = fetchFalso([{ status: 200, cuerpo: { cursos: [] } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'tok', fetchImpl: f.impl });

    await expect(api.estudiante.matriz()).resolves.toEqual({ cursos: [] });
    expect(f.llamadas[0].url).toBe('/api/mis-notas');
    expect((f.llamadas[0].init.headers as Record<string, string>).Authorization).toBe('Bearer tok');
  });

  it('sube con multipart en el campo "archivo" y edita y anula por id', async () => {
    const f = fetchFalso([{ status: 201, cuerpo: entrega }, { status: 200, cuerpo: entrega }, { status: 200, cuerpo: { ...entrega, estado: 'ANULADA' } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'tok', fetchImpl: f.impl });
    const archivo = { nombre: 't.pdf', tamano: 10, datos: new Blob(['%PDF-1.7']) };

    await api.estudiante.subirEntrega('d1', archivo);
    await api.estudiante.editarEntrega('x1', archivo);
    const anulada = await api.estudiante.anularEntrega('x1');

    expect(f.llamadas.map((l) => `${l.init.method} ${l.url}`)).toEqual(['POST /api/actividades/d1/entregas', 'PUT /api/entregas/x1', 'DELETE /api/entregas/x1']);
    expect((f.llamadas[0].init.body as FormData).get('archivo')).toBeInstanceOf(Blob);
    expect(anulada.estado).toBe('ANULADA');
  });

  it('convierte el error estándar del backend en ApiError con su código y mensaje', async () => {
    const f = fetchFalso([{ status: 422, cuerpo: { status: 422, codigo: 'FECHA_LIMITE_VENCIDA', mensaje: 'La fecha límite de la actividad ya venció.', traceId: 't' } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'tok', fetchImpl: f.impl });

    const e = await api.estudiante.subirEntrega('d3', { nombre: 'p.pdf', tamano: 1, datos: new Blob(['x']) }).catch((x) => x);
    expect(e).toBeInstanceOf(ApiError);
    expect([e.status, e.codigo, e.message]).toEqual([422, 'FECHA_LIMITE_VENCIDA', 'La fecha límite de la actividad ya venció.']);
  });

  it('rechaza respuestas que no cumplen el contrato', async () => {
    const f = fetchFalso([{ status: 200, cuerpo: { cursos: [{ cursoId: 'c1' }] } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => null, fetchImpl: f.impl });

    await expect(api.estudiante.matriz()).rejects.toThrow();
  });
});
