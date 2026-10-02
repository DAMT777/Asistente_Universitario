import { describe, expect, it } from 'vitest';
import { createMockApi } from './mockApi';

const docente = { usuario: 'lrincon', contrasena: 'demo123', rol: 'docente' as const };
const estudiante = { usuario: '160005017', contrasena: 'demo123', rol: 'estudiante' as const };
const nuevo = () => createMockApi({ hoy: () => '2026-10-01T17:00:00Z', latenciaMs: 0 });
const corte2 = { cursoId: 'sim', estudianteId: 'e1', corte: 2 as const, omitirBorradores: false, corregir: false };

describe('publicación de cortes por estudiante', () => {
  it('publicar una actividad no publica el corte y la corrección conserva los demás estudiantes', async () => {
    const api = nuevo(); await api.auth.login(docente);
    await api.calificaciones.guardar({ actividadId: 's3', estudianteId: 'e1', nota: 4, retro: '', publicar: true });
    expect((await api.cortes.listar('sim')).some(p => p.corte === 2)).toBe(false);
    const antes = await api.cortes.listar('sim');
    expect((await api.cortes.publicar(corte2)).nota).toBe(2);
    await api.calificaciones.guardar({ actividadId: 's3', estudianteId: 'e1', nota: 2, retro: '', publicar: true });
    expect((await api.cortes.listar('sim')).find(p => p.corte === 2)?.nota).toBe(2);
    expect((await api.cortes.publicar({ ...corte2, corregir: true })).nota).toBe(1);
    expect((await api.cortes.listar('sim')).filter(p => p.estudianteId !== 'e1')).toEqual(antes.filter(p => p.estudianteId !== 'e1'));
  });
  it('rechaza borradores salvo omisión expresa y conserva la nota cero', async () => {
    const api = nuevo(); await api.auth.login(docente);
    await api.calificaciones.guardar({ actividadId: 's3', estudianteId: 'e1', nota: 4, retro: '', publicar: false });
    await api.calificaciones.guardar({ actividadId: 's4', estudianteId: 'e1', nota: 0, retro: '', publicar: true });
    await expect(api.cortes.publicar(corte2)).rejects.toThrow('borrador');
    expect((await api.cortes.publicar({ ...corte2, omitirBorradores: true })).nota).toBe(0);
    await expect(api.cortes.publicar({ ...corte2, estudianteId: 'e5' })).rejects.toThrow('no tiene calificaciones');
  });
  it('el estudiante solo ve sus notas publicadas y no puede publicar cortes', async () => {
    const api = nuevo(); await api.auth.login(docente);
    await api.calificaciones.guardar({ actividadId: 's3', estudianteId: 'e1', nota: 4, retro: 'Privado', publicar: false });
    await api.auth.login(estudiante);
    expect((await api.cortes.listar()).every(p => p.estudianteId === 'e1')).toBe(true);
    expect((await api.calificaciones.delEstudiante('e1')).find(c => c.actividadId === 's3')).toMatchObject({ nota: null, retro: '', estado: null });
    await expect(api.calificaciones.delEstudiante('e2')).rejects.toThrow('propias');
    await expect(api.calificaciones.porCurso('sim')).rejects.toThrow('docente');
    await expect(api.cortes.publicar(corte2)).rejects.toThrow('docente');
  });
});

describe('entregas y pesos en la API simulada', () => {
  it('permite reemplazar y anular después de calificar, conserva nota y permite volver a entregar', async () => {
    const api = nuevo(); await api.auth.login(docente);
    await api.calificaciones.guardar({ actividadId: 's4', estudianteId: 'e1', nota: 4, retro: 'Revisión', publicar: true });
    await api.auth.login(estudiante);
    await api.calificaciones.entregar('s4', { nombre: 'reporte.xlsx', tamano: 20 });
    await api.calificaciones.anularEntrega('s4');
    expect((await api.calificaciones.delEstudiante('e1')).find(c => c.actividadId === 's4')).toMatchObject({ nota: 4, entregado: null, archivo: null });
    expect((await api.calificaciones.entregar('s4', { nombre: 'nuevo.pdf', tamano: 50 })).archivo).toBe('nuevo.pdf');
  });
  it('rechaza entrega vencida, anulación vencida y archivo para un parcial', async () => {
    const api = nuevo(); await api.auth.login(estudiante);
    await expect(api.calificaciones.entregar('s3', { nombre: 'x.pdf', tamano: 1 })).rejects.toThrow('cerró');
    await expect(api.calificaciones.anularEntrega('s3')).rejects.toThrow('cerró');
    await expect(api.calificaciones.entregar('t3', { nombre: 'x.pdf', tamano: 1 })).rejects.toThrow('no requiere');
  });
  it('valida pesos antes de modificar y permite completar actividades por etapas', async () => {
    const api = nuevo(); await api.auth.login(docente);
    await api.cursos.actualizarPesos('sim', { cortes: [30, 30, 40], actividades: { s5: 40 } });
    await api.actividades.crear({ cursoId: 'sim', corte: 3, titulo: 'Sustentación', peso: 20, vence: '', requiereEntrega: false });
    await expect(api.actividades.crear({ cursoId: 'sim', corte: 3, titulo: 'Otro parcial', peso: 50, vence: '', requiereEntrega: false })).rejects.toThrow('100%');
    await expect(api.cursos.actualizarPesos('sim', { cortes: [30, 30, 30], actividades: {} })).rejects.toThrow('100%');
    expect((await api.cursos.listar()).find(c => c.id === 'sim')?.pesos).toEqual([30, 30, 40]);
  });
});
