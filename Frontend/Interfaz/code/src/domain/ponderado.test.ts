import { describe, expect, it } from 'vitest';
import type { Actividad, Calificacion, Curso } from '@/types';
import { calcularNecesidad, formatoNota, resumenCurso } from './ponderado';
import { estadoDocente, estadoEstudiante } from './estados';
import { validarPesos } from './reglas';
import { notaSchema } from '@/schemas';

const curso: Curso = {
  id: 'sim', codigo: '603803', nombre: 'Simulación', grupo: 1, creditos: 3, periodo: '2026B',
  docente: 'L', pesos: [30, 30, 40], monograma: 'SC', acento: 'rojo',
};
const acts: Actividad[] = [
  { id: 's1', cursoId: 'sim', corte: 1, titulo: 'T1', peso: 40, vence: '2026-08-28' },
  { id: 's2', cursoId: 'sim', corte: 1, titulo: 'Q1', peso: 60, vence: '2026-09-05' },
  { id: 's3', cursoId: 'sim', corte: 2, titulo: 'T2', peso: 50, vence: '2026-09-26' },
];
const cal = (actividadId: string, nota: number | null, estado: Calificacion['estado']): Calificacion => ({
  actividadId, estudianteId: 'e1', entregado: '2026-08-27', archivo: 'x.pdf', nota, estado, retro: '',
});

describe('resumenCurso', () => {
  it('pondera el corte 1 y calcula el aporte', () => {
    const r = resumenCurso(curso, acts, [cal('s1', 4.0, 'publicada'), cal('s2', 4.5, 'publicada')]);
    expect(r.cortes[0].nota).toBeCloseTo(4.3);
    expect(r.acumulado).toBeCloseTo(1.29);
    expect(formatoNota(r.acumulado)).toBe('1.3');
    expect(r.evaluado).toBe(30);
  });

  it('ignora borradores', () => {
    const r = resumenCurso(curso, acts, [cal('s1', 4.0, 'publicada'), cal('s3', 5.0, 'borrador')]);
    expect(r.cortes[1].nota).toBeNull();
    expect(r.evaluado).toBe(12);
  });
});

describe('calcularNecesidad', () => {
  it('necesita nota en lo restante', () => {
    const n = calcularNecesidad(1.29, 30);
    expect(n.tipo).toBe('necesita');
    if (n.tipo === 'necesita') expect(n.nota).toBeCloseTo(2.443, 2);
  });
  it('aprobado e inalcanzable', () => {
    expect(calcularNecesidad(3.1, 70).tipo).toBe('aprobado');
    expect(calcularNecesidad(0.5, 90).tipo).toBe('inalcanzable');
  });
});

describe('estados', () => {
  const a = acts[2];
  it('docente distingue borrador; estudiante no', () => {
    const c = cal('s3', 4, 'borrador');
    expect(estadoDocente(a, c, '2026-10-01')).toBe('borrador');
    expect(estadoEstudiante(a, c, '2026-10-01')).toBe('entregada');
  });
  it('vencida sin entrega', () => {
    expect(estadoEstudiante(a, undefined, '2026-10-01')).toBe('vencida');
    expect(estadoEstudiante(a, undefined, '2026-09-20')).toBe('pendiente');
  });
});

describe('validaciones', () => {
  it('pesos suman 100', () => {
    expect(validarPesos([30, 30, 40]).ok).toBe(true);
    expect(validarPesos([30, 30, 30]).ok).toBe(false);
    expect(validarPesos(['50', '0', '50']).ok).toBe(false);
  });
  it('nota con un decimal entre 0 y 5', () => {
    expect(notaSchema.parse('3,5')).toBe(3.5);
    expect(notaSchema.safeParse('5.5').success).toBe(false);
    expect(notaSchema.safeParse('3.25').success).toBe(false);
  });
});
