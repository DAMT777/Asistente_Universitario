import { describe, expect, it } from 'vitest';
import { haceCuanto } from '@/domain';
import { rutaNotificacion } from './rutaNotificacion';

describe('presentación de avisos', () => {
  const ahora = Date.parse('2026-11-19T23:00:00Z');
  it('dice hace cuánto llegó', () => {
    expect(haceCuanto('2026-11-19T22:59:40Z', ahora)).toBe('Ahora');
    expect(haceCuanto('2026-11-19T22:45:00Z', ahora)).toBe('Hace 15 min');
    expect(haceCuanto('2026-11-19T20:00:00Z', ahora)).toBe('Hace 3 h');
    expect(haceCuanto('2026-11-18T20:00:00Z', ahora)).toBe('Ayer');
    expect(haceCuanto('2026-11-15T20:00:00Z', ahora)).toBe('Hace 4 días');
    expect(haceCuanto('2026-09-30T20:00:00Z', ahora)).toMatch(/30/);
  });
  it('lleva a la actividad, a las notas o al curso', () => {
    expect(rutaNotificacion({ tipo: 'nueva_actividad', cursoId: 'sim', actividadId: 's5' }, 'estudiante')).toBe('/e/cursos/sim/actividades/s5');
    expect(rutaNotificacion({ tipo: 'nota_publicada', cursoId: 'sim', actividadId: null }, 'estudiante')).toBe('/e/notas');
    expect(rutaNotificacion({ tipo: 'curso_actualizado', cursoId: 'eti', actividadId: null }, 'estudiante')).toBe('/e/cursos/eti');
    expect(rutaNotificacion({ tipo: 'nota_publicada', cursoId: 'sim', actividadId: 's1' }, 'docente')).toBe('/d/cursos/sim');
  });
});
