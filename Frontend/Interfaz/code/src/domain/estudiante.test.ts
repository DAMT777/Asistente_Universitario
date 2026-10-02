import { describe, expect, it } from 'vitest';
import type { CursoMatriz, Entrega, NotaActividad } from '@/types';
import { actividadDesdeNota, calificacionDesde, cursoDesdeMatriz, monograma, resumenDesdeMatriz } from './estudiante';
import { estadoEstudiante } from './estados';
import { formatoNota } from './ponderado';

/** Respuesta real de GET /mis-notas para Ana con la semilla del backend. */
const ana: CursoMatriz = {
  cursoId: 'c1', codigo: '603803', nombre: 'Simulación Computacional', profesor: 'Laura Rincón',
  cortes: [
    { corte: 1, pesoCorte: 30, nota: 3.2, publicado: true },
    { corte: 2, pesoCorte: 30, nota: null, publicado: false },
    { corte: 3, pesoCorte: 40, nota: null, publicado: false },
  ],
  definitivaParcial: 1.0, esParcial: true,
};

const nota = (extra: Partial<NotaActividad> = {}): NotaActividad => ({
  actividadId: 'd1', titulo: 'Taller 1', corte: 1, peso: 20, fechaLimite: '2026-10-31T23:59:59Z',
  estado: 'SIN_CALIFICAR', nota: null, retroalimentacion: null, ...extra,
});
const entrega = (estado: Entrega['estado']): Entrega => ({
  id: 'x1', actividadId: 'd1', estudianteId: 'e1', fechaEnvio: '2026-10-01T15:00:00Z', estado,
  archivos: [{ id: 'a1', nombreArchivo: 'taller.pdf', tamano: 10 }, { id: 'a2', nombreArchivo: 'anexo.png', tamano: 5 }], tamanoTotal: 15,
});

describe('resumenDesdeMatriz', () => {
  it('usa la nota publicada del corte y la definitiva del backend (E-13)', () => {
    const r = resumenDesdeMatriz(ana);
    expect(r.cortes.map((k) => k.nota)).toEqual([3.2, null, null]);
    expect(formatoNota(r.acumulado)).toBe('1.0');
    expect(r.evaluado).toBe(30);
    expect(r.necesidad).toEqual({ tipo: 'necesita', nota: expect.closeTo(2.857, 3), restante: 70 });
  });

  it('sin cortes publicados el acumulado es 0 y nada está evaluado (E-14)', () => {
    const marta = { ...ana, definitivaParcial: 0, cortes: ana.cortes.map((k) => ({ ...k, nota: null, publicado: false })) };
    const r = resumenDesdeMatriz(marta);
    expect(r.cortes.every((k) => k.nota === null)).toBe(true);
    expect([r.acumulado, r.evaluado]).toEqual([0, 0]);
  });
});

describe('cursoDesdeMatriz', () => {
  it('toma pesos por corte, profesor y un monograma', () => {
    const c = cursoDesdeMatriz(ana, 1);
    expect(c).toMatchObject({ id: 'c1', docente: 'Laura Rincón', pesos: [30, 30, 40], monograma: 'SC', acento: 'violeta' });
    expect(c.creditos).toBeUndefined();
    expect(monograma('Ética y Humanística')).toBe('EH');
  });
});

describe('calificacionDesde', () => {
  it('un borrador o sin calificar sin entrega no es calificación (nunca 0)', () => {
    expect(calificacionDesde(nota(), undefined, 'e1')).toBeUndefined();
  });

  it('con entrega enviada la actividad queda "entregada"', () => {
    const n = nota();
    const c = calificacionDesde(n, entrega('ENVIADA'), 'e1');
    expect(c).toMatchObject({ archivo: 'taller.pdf, anexo.png', nota: null, estado: null });
    expect(estadoEstudiante(actividadDesdeNota('c1', n), c, '2026-10-01')).toBe('entregada');
  });

  it('una entrega anulada vuelve a dejar la actividad pendiente', () => {
    const n = nota();
    const c = calificacionDesde(n, entrega('ANULADA'), 'e1');
    expect(c).toBeUndefined();
    expect(estadoEstudiante(actividadDesdeNota('c1', n), c, '2026-10-01')).toBe('pendiente');
  });

  it('una nota publicada se muestra con su retroalimentación', () => {
    const c = calificacionDesde(nota({ estado: 'PUBLICADA', nota: 4.0, retroalimentacion: 'Bien' }), undefined, 'e1');
    expect(c).toMatchObject({ nota: 4.0, estado: 'publicada', retro: 'Bien', entregado: null });
  });
});
