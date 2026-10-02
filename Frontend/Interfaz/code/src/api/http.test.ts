import { describe, expect, it } from 'vitest';
import { createHttpApi, nombreDeDisposicion } from './http';
import { ApiError } from './contratos';

interface Llamada { url: string; init: RequestInit }

function fetchFalso(respuestas: Array<{ status: number; cuerpo?: unknown; binario?: Blob; encabezados?: Record<string, string> }>) {
  const llamadas: Llamada[] = [];
  const impl = (async (url: string, init: RequestInit) => {
    llamadas.push({ url, init });
    const r = respuestas.shift()!;
    const cuerpo = r.binario ?? (r.cuerpo === undefined ? null : JSON.stringify(r.cuerpo));
    return new Response(cuerpo, { status: r.status, headers: r.encabezados });
  }) as unknown as typeof fetch;
  return { impl, llamadas };
}

const entrega = {
  id: 'x1', actividadId: 'd1', estudianteId: 'e1', fechaEnvio: '2026-10-01T15:00:00Z', estado: 'ENVIADA',
  archivos: [{ id: 'a1', nombreArchivo: 't.pdf', tamano: 8 }, { id: 'a2', nombreArchivo: 'b.png', tamano: 2 }], tamanoTotal: 10,
};

describe('createHttpApi · estudiante', () => {
  it('pide la matriz con el token y valida la respuesta', async () => {
    const f = fetchFalso([{ status: 200, cuerpo: { cursos: [] } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'tok', fetchImpl: f.impl });

    await expect(api.estudiante.matriz()).resolves.toEqual({ cursos: [] });
    expect(f.llamadas[0].url).toBe('/api/mis-notas');
    expect((f.llamadas[0].init.headers as Record<string, string>).Authorization).toBe('Bearer tok');
  });

  it('sube varios archivos repitiendo el campo "archivo" y edita y anula por id', async () => {
    const f = fetchFalso([{ status: 201, cuerpo: entrega }, { status: 200, cuerpo: entrega }, { status: 200, cuerpo: { ...entrega, estado: 'ANULADA' } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'tok', fetchImpl: f.impl });
    const archivos = [{ nombre: 't.pdf', tamano: 8, datos: new Blob(['%PDF-1.7']) }, { nombre: 'b.png', tamano: 2, datos: new Blob(['xx']) }];

    const subida = await api.estudiante.subirEntrega('d1', archivos);
    await api.estudiante.editarEntrega('x1', archivos.slice(0, 1));
    const anulada = await api.estudiante.anularEntrega('x1');

    expect(f.llamadas.map((l) => `${l.init.method} ${l.url}`)).toEqual(['POST /api/actividades/d1/entregas', 'PUT /api/entregas/x1', 'DELETE /api/entregas/x1']);
    const campos = (f.llamadas[0].init.body as FormData).getAll('archivo') as File[];
    expect(campos.map((x) => x.name)).toEqual(['t.pdf', 'b.png']);
    expect((f.llamadas[1].init.body as FormData).getAll('archivo')).toHaveLength(1);
    expect(subida.archivos.map((a) => a.nombreArchivo)).toEqual(['t.pdf', 'b.png']);
    expect(anulada.estado).toBe('ANULADA');
  });

  it('descarga el archivo como Blob con el nombre de Content-Disposition', async () => {
    const f = fetchFalso([{ status: 200, binario: new Blob(['%PDF-1.7 hola']), encabezados: { 'Content-Disposition': "attachment; filename=Taller_1.pdf; filename*=UTF-8''Taller%201%20%C3%B1.pdf" } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'tok', fetchImpl: f.impl });

    const r = await api.estudiante.descargarArchivo('x1', 'a1');

    expect(f.llamadas[0].url).toBe('/api/entregas/x1/archivos/a1');
    expect(r.nombre).toBe('Taller 1 ñ.pdf');
    expect(await r.datos.text()).toBe('%PDF-1.7 hola');
  });

  it('lee el nombre de Content-Disposition en sus dos formas', () => {
    expect(nombreDeDisposicion('attachment; filename="informe final.docx"')).toBe('informe final.docx');
    expect(nombreDeDisposicion("attachment; filename*=UTF-8''c%C3%B3digo.zip")).toBe('código.zip');
    expect(nombreDeDisposicion(null)).toBeNull();
  });

  it('convierte el error estándar del backend en ApiError con su código y mensaje', async () => {
    const f = fetchFalso([{ status: 422, cuerpo: { status: 422, codigo: 'FECHA_LIMITE_VENCIDA', mensaje: 'La fecha límite de la actividad ya venció.', traceId: 't' } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => 'tok', fetchImpl: f.impl });

    const e = await api.estudiante.subirEntrega('d3', [{ nombre: 'p.pdf', tamano: 1, datos: new Blob(['x']) }]).catch((x) => x);
    expect(e).toBeInstanceOf(ApiError);
    expect([e.status, e.codigo, e.message]).toEqual([422, 'FECHA_LIMITE_VENCIDA', 'La fecha límite de la actividad ya venció.']);
  });

  it('rechaza respuestas que no cumplen el contrato', async () => {
    const f = fetchFalso([{ status: 200, cuerpo: { cursos: [{ cursoId: 'c1' }] } }]);
    const api = createHttpApi({ baseUrl: '/api', getToken: () => null, fetchImpl: f.impl });

    await expect(api.estudiante.matriz()).rejects.toThrow();
  });
});
