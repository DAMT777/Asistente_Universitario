import type { Actividad, Calificacion, EstadoDocente, EstadoEstudiante } from '@/types';

export function estadoDocente(a: Actividad, c: Calificacion | undefined, hoy: string): EstadoDocente {
  if (c?.estado === 'publicada') return 'publicada';
  if (c?.estado === 'borrador') return 'borrador';
  if (c?.entregado) return 'sin_calificar';
  if (!a.requiereEntrega) return 'sin_calificar';
  return a.vence && Date.parse(a.vence) < Date.parse(hoy) ? 'vencida' : 'pendiente';
}

/** El estudiante no distingue borrador de "sin calificar": ambos son "entregada". */
export function estadoEstudiante(a: Actividad, c: Calificacion | undefined, hoy: string): EstadoEstudiante {
  if (c?.estado === 'publicada') return 'calificada';
  if (c?.entregado) return 'entregada';
  if (!a.requiereEntrega) return 'pendiente';
  return a.vence && Date.parse(a.vence) < Date.parse(hoy) ? 'vencida' : 'pendiente';
}

export const ETIQUETA_ESTADO: Record<EstadoDocente | EstadoEstudiante, string> = {
  pendiente: 'Pendiente',
  vencida: 'Vencida',
  sin_calificar: 'Sin calificar',
  borrador: 'Borrador',
  publicada: 'Publicada',
  entregada: 'Entregada',
  calificada: 'Calificada',
};

export interface ConteoActividad {
  entregas: number;
  sinCalificar: number;
  borradores: number;
  publicadas: number;
}

export function contarActividad(a: Actividad, califs: Calificacion[], estudiantes: string[], hoy: string): ConteoActividad {
  const r: ConteoActividad = { entregas: 0, sinCalificar: 0, borradores: 0, publicadas: 0 };
  for (const id of estudiantes) {
    const c = califs.find((x) => x.actividadId === a.id && x.estudianteId === id);
    const e = estadoDocente(a, c, hoy);
    if (c?.entregado) r.entregas++;
    if (e === 'sin_calificar') r.sinCalificar++;
    if (e === 'borrador') r.borradores++;
    if (e === 'publicada') r.publicadas++;
  }
  return r;
}
