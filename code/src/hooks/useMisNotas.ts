import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { Actividad, Calificacion, Curso, EstadoEstudiante, ResumenCurso } from '@/types';
import { describirNecesidad, estadoEstudiante, resumenCurso } from '@/domain';
import { useApi } from './ApiContext';
import { useUsuario } from './useSesion';
import { useActividades, useCursos } from './useCursos';
import { qk } from './queryKeys';

export interface ActividadEstudiante {
  actividad: Actividad;
  calificacion: Calificacion | undefined;
  estado: EstadoEstudiante;
}

export interface NotasCurso {
  curso: Curso;
  resumen: ResumenCurso;
  mensaje: string;
  actividades: ActividadEstudiante[];
  entregas: number;
}

export function useMisCalificaciones() {
  const { api } = useApi();
  const u = useUsuario();
  return useQuery({ queryKey: qk.califsEstudiante(u.id), queryFn: () => api.calificaciones.delEstudiante(u.id) });
}

/** Resumen ponderado de todos los cursos del estudiante en sesión. */
export function useMisNotas() {
  const { hoy, aprobatoria } = useApi();
  const cursos = useCursos();
  const acts = useActividades();
  const califs = useMisCalificaciones();

  const data = useMemo<NotasCurso[] | undefined>(() => {
    if (!cursos.data || !acts.data || !califs.data) return undefined;
    const h = hoy();
    return cursos.data.map((curso) => {
      const propias = acts.data.filter((a) => a.cursoId === curso.id);
      const resumen = resumenCurso(curso, propias, califs.data, aprobatoria);
      const actividades = propias.map((actividad) => {
        const calificacion = califs.data.find((c) => c.actividadId === actividad.id);
        return { actividad, calificacion, estado: estadoEstudiante(actividad, calificacion, h) };
      });
      return { curso, resumen, mensaje: describirNecesidad(resumen.necesidad, aprobatoria), actividades, entregas: actividades.filter((a) => a.calificacion?.entregado).length };
    });
  }, [cursos.data, acts.data, califs.data, hoy, aprobatoria]);

  return {
    data,
    totalCreditos: cursos.data?.reduce((a, c) => a + c.creditos, 0) ?? 0,
    isLoading: cursos.isLoading || acts.isLoading || califs.isLoading,
    error: cursos.error ?? acts.error ?? califs.error,
    refetch: () => Promise.all([cursos.refetch(), acts.refetch(), califs.refetch()]),
  };
}

export function useNotasCurso(cursoId: string | undefined) {
  const q = useMisNotas();
  return { ...q, data: q.data?.find((n) => n.curso.id === cursoId) };
}

/** Actividades con fecha límite desde hoy, ordenadas por cercanía. */
export function useProximasEntregas(limite = 4) {
  const { hoy } = useApi();
  const q = useMisNotas();
  const data = useMemo(() => {
    if (!q.data) return undefined;
    const h = hoy();
    return q.data
      .flatMap((n) => n.actividades.map((a) => ({ ...a, curso: n.curso })))
      .filter((a) => a.actividad.vence >= h)
      .sort((a, b) => a.actividad.vence.localeCompare(b.actividad.vence))
      .slice(0, limite);
  }, [q.data, hoy, limite]);
  return { ...q, data };
}
