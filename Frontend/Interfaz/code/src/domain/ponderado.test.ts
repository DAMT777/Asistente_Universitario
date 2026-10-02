import { describe, expect, it } from 'vitest';
import type { Actividad, Calificacion, Curso } from '@/types';
import { calcularNecesidad, formatoNota, resumenCurso } from './ponderado';
import { estadoDocente, estadoEstudiante } from './estados';
import { puedeEntregar, validarPesos } from './reglas';
import { archivoEntregaSchema, notaSchema, TAMANO_MAX_ENTREGA } from '@/schemas';

const curso: Curso = {
  id: 'sim', codigo: '603803', nombre: 'Simulación', grupo: 1, creditos: 3, periodo: '2026B',
  docente: 'L', pesos: [30, 30, 40], monograma: 'SC', acento: 'rojo',
};
const acts: Actividad[] = [
  { id: 's1', cursoId: 'sim', corte: 1, titulo: 'T1', peso: 40, vence: '2026-08-28', requiereEntrega: true },
  { id: 's2', cursoId: 'sim', corte: 1, titulo: 'Q1', peso: 60, vence: '2026-09-05', requiereEntrega: true },
  { id: 's3', cursoId: 'sim', corte: 2, titulo: 'T2', peso: 50, vence: '2026-09-26', requiereEntrega: true },
];
const cal = (actividadId: string, nota: number | null, estado: Calificacion['estado']): Calificacion => ({
  actividadId, estudianteId: 'e1', entregado: '2026-08-27', archivo: 'x.pdf', nota, estado, retro: '',
});

describe('resumenCurso', () => {
  it('acumula sin normalizar un corte incompleto', () => {
    const r = resumenCurso(curso, [{ ...acts[0], peso: 20 }], [cal('s1', 4, 'publicada')]);
    expect(r.cortes[0].nota).toBeCloseTo(0.8);
    expect(r.acumulado).toBeCloseTo(0.24);
  });
  it('distingue cero publicado de una actividad sin calificar', () => {
    expect(resumenCurso(curso, acts, [cal('s1', 0, 'publicada')]).cortes[0].nota).toBe(0);
    expect(resumenCurso(curso, acts, []).cortes[0].nota).toBeNull();
  });
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
  it('una actividad sin archivo ni fecha no aparece como vencida', () => {
    const actividad = { ...acts[0], requiereEntrega: false, vence: '' };
    expect(estadoEstudiante(actividad, undefined, '2026-10-01T17:00:00Z')).toBe('pendiente');
    expect(estadoDocente(actividad, undefined, '2026-10-01T17:00:00Z')).toBe('sin_calificar');
  });
  it('compara instantes en zonas horarias distintas al determinar el vencimiento', () => {
    const actividad = { ...acts[0], vence: '2026-10-01T15:00:00-05:00' };
    expect(estadoEstudiante(actividad, undefined, '2026-10-01T19:59:00Z')).toBe('pendiente');
    expect(estadoEstudiante(actividad, undefined, '2026-10-01T20:00:01Z')).toBe('vencida');
  });
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
  it('permite un corte de actividades incompleto y rechaza exceder 100', () => {
    expect(validarPesos([20, 30], false).ok).toBe(true);
    expect(validarPesos([60, 50], false).ok).toBe(false);
    expect(validarPesos([0, 20], false).ok).toBe(false);
  });
  it('acepta la hora límite inclusive y bloquea actividades sin entrega', () => {
    const limite = '2026-10-01T20:00:00Z';
    expect(puedeEntregar(limite, '2026-10-01T15:00:00-05:00', true)).toBe(true);
    expect(puedeEntregar(limite, '2026-10-01T20:00:00.001Z', true)).toBe(false);
    expect(puedeEntregar(limite, limite, false)).toBe(false);
  });
  it('valida formatos, archivos vacíos y el límite de 20 MB', () => {
    for (const ext of ['pdf', 'docx', 'xlsx', 'pptx', 'zip', 'png', 'jpg']) expect(archivoEntregaSchema.safeParse({ nombre: `archivo.${ext}`, tamano: TAMANO_MAX_ENTREGA }).success).toBe(true);
    expect(archivoEntregaSchema.safeParse({ nombre: 'archivo.pdf', tamano: TAMANO_MAX_ENTREGA + 1 }).success).toBe(false);
    expect(archivoEntregaSchema.safeParse({ nombre: 'archivo.exe', tamano: 100 }).success).toBe(false);
    expect(archivoEntregaSchema.safeParse({ nombre: 'archivo.pdf', tamano: 0 }).success).toBe(false);
  });
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
