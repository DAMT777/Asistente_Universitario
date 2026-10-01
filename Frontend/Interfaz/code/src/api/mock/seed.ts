import type { Actividad, Calificacion, Curso, Usuario } from '@/types';

export const CURSOS: Curso[] = [
  { id: 'sim', codigo: '603803', nombre: 'Simulación Computacional', grupo: 1, creditos: 3, periodo: '2026B', docente: 'Laura Rincón', pesos: [30, 30, 40], monograma: 'SC', acento: 'rojo' },
  { id: 'arq', codigo: '603804', nombre: 'Arquitectura Empresarial', grupo: 1, creditos: 3, periodo: '2026B', docente: 'Laura Rincón', pesos: [35, 35, 30], monograma: 'AE', acento: 'violeta' },
  { id: 'eti', codigo: '603703', nombre: 'Ética y Humanística', grupo: 1, creditos: 2, periodo: '2026B', docente: 'Laura Rincón', pesos: [33, 33, 34], monograma: 'EH', acento: 'verde' },
];

export const DOCENTE: Usuario = { id: 'd1', nombre: 'Laura Rincón', codigo: 'lrincon', rol: 'docente' };

export const ESTUDIANTES: Usuario[] = [
  { id: 'e1', nombre: 'Diego Alejandro Machado Tovar', codigo: '160005017', rol: 'estudiante' },
  { id: 'e2', nombre: 'Valentina Ortiz Rojas', codigo: '160005021', rol: 'estudiante' },
  { id: 'e3', nombre: 'Juan Sebastián Rey', codigo: '160005034', rol: 'estudiante' },
  { id: 'e4', nombre: 'Mariana López Gaitán', codigo: '160005040', rol: 'estudiante' },
  { id: 'e5', nombre: 'Camilo Herrera Díaz', codigo: '160005052', rol: 'estudiante' },
  { id: 'e6', nombre: 'Daniela Pardo Silva', codigo: '160005067', rol: 'estudiante' },
];

export const ACTIVIDADES: Actividad[] = [
  { id: 's1', cursoId: 'sim', corte: 1, titulo: 'Taller 1 · Método Montecarlo', peso: 40, vence: '2026-08-28' },
  { id: 's2', cursoId: 'sim', corte: 1, titulo: 'Quiz 1 · Generadores aleatorios', peso: 60, vence: '2026-09-05' },
  { id: 's3', cursoId: 'sim', corte: 2, titulo: 'Taller 2 · Teoría de colas', peso: 50, vence: '2026-09-26' },
  { id: 's4', cursoId: 'sim', corte: 2, titulo: 'Avance de proyecto', peso: 50, vence: '2026-10-10' },
  { id: 's5', cursoId: 'sim', corte: 3, titulo: 'Proyecto final', peso: 100, vence: '2026-11-20' },
  { id: 'a1', cursoId: 'arq', corte: 1, titulo: 'Ensayo · Marco TOGAF', peso: 100, vence: '2026-09-04' },
  { id: 'a2', cursoId: 'arq', corte: 2, titulo: 'Caso de estudio · ArchiMate', peso: 60, vence: '2026-09-29' },
  { id: 'a3', cursoId: 'arq', corte: 2, titulo: 'Exposición grupal', peso: 40, vence: '2026-10-15' },
  { id: 'a4', cursoId: 'arq', corte: 3, titulo: 'Examen final', peso: 100, vence: '2026-11-25' },
  { id: 't1', cursoId: 'eti', corte: 1, titulo: 'Reflexión escrita', peso: 50, vence: '2026-08-30' },
  { id: 't2', cursoId: 'eti', corte: 1, titulo: 'Foro · Dilemas profesionales', peso: 50, vence: '2026-09-08' },
  { id: 't3', cursoId: 'eti', corte: 2, titulo: 'Debate en clase', peso: 100, vence: '2026-10-08' },
  { id: 't4', cursoId: 'eti', corte: 3, titulo: 'Ensayo final', peso: 100, vence: '2026-11-28' },
];

export function calificacionesIniciales(): Calificacion[] {
  const out: Calificacion[] = [];
  const base = [[4.0, 4.5, 3.6, 5.0, 5.0], [3.8, 4.2, 4.1, 4.6, 4.4], [2.9, 3.3, 3.0, 4.0, 3.8], [4.6, 4.8, 4.4, 4.9, 4.7], [3.2, 2.7, 3.5, 3.9, 4.1], [4.1, 3.9, 2.8, 4.3, 4.5]];
  const retroDiego: Record<string, string> = {
    s1: 'Buen planteamiento del experimento. Faltó justificar el número de réplicas.',
    s2: 'Excelente manejo de la prueba de chi-cuadrado.',
    a1: 'Argumentación clara. Revisa las citas en formato APA.',
    t1: 'Reflexión madura y bien escrita.',
  };
  ESTUDIANTES.forEach((e, i) =>
    ['s1', 's2', 'a1', 't1', 't2'].forEach((a, j) =>
      out.push({ actividadId: a, estudianteId: e.id, entregado: '2026-08-27', archivo: `${a}_${e.codigo}.pdf`, nota: base[i][j], estado: 'publicada', retro: i === 0 ? retroDiego[a] ?? '' : '' }),
    ),
  );
  const ent = (a: string, e: string, extra: Partial<Calificacion> = {}) => {
    const cod = ESTUDIANTES.find((x) => x.id === e)!.codigo;
    out.push({ actividadId: a, estudianteId: e, entregado: '2026-09-24', archivo: `${a}_${cod}.pdf`, nota: null, estado: null, retro: '', ...extra });
  };
  ent('s3', 'e1'); ent('s3', 'e2', { nota: 4.2, estado: 'borrador' }); ent('s3', 'e3'); ent('s3', 'e4', { nota: 4.7, estado: 'borrador' }); ent('s3', 'e6');
  ent('a2', 'e1', { nota: 3.9, estado: 'publicada', retro: 'El modelo de capas está completo. Mejora la vista de motivación.' });
  ent('a2', 'e2', { nota: 4.3, estado: 'publicada' }); ent('a2', 'e3'); ent('a2', 'e4', { nota: 4.5, estado: 'publicada' }); ent('a2', 'e5');
  ent('s4', 'e4');
  return out;
}
