import type { Notificacion, Rol } from '@/types';

/** A dónde lleva cada aviso: la actividad si la hay, las notas si es de un corte o el curso si cambió algo general. */
export function rutaNotificacion(n: Pick<Notificacion, 'tipo' | 'cursoId' | 'actividadId'>, rol: Rol): string {
  if (rol === 'docente') return `/d/cursos/${n.cursoId}`;
  if (n.actividadId) return `/e/cursos/${n.cursoId}/actividades/${n.actividadId}`;
  return n.tipo === 'nota_publicada' ? '/e/notas' : `/e/cursos/${n.cursoId}`;
}
