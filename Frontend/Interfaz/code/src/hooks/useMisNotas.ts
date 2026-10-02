import { useMemo } from 'react';
import { useQueries, useQuery, type UseQueryResult } from '@tanstack/react-query';
import type { Actividad, Calificacion, Curso, EstadoEstudiante, Entrega, NotasActividades, ResumenCurso } from '@/types';
import { actividadDesdeNota, calificacionDesde, cursoDesdeMatriz, describirNecesidad, estadoEstudiante, resumenDesdeMatriz } from '@/domain';
import { useApi } from './ApiContext';
import { useUsuario } from './useSesion';
import { qk } from './queryKeys';

export interface ActividadEstudiante {
  actividad: Actividad;
  calificacion: Calificacion | undefined;
  /** Entrega propia (enviada o anulada), si existe. */
  entrega: Entrega | undefined;
  estado: EstadoEstudiante;
}

export interface NotasCurso {
  curso: Curso;
  resumen: ResumenCurso;
  mensaje: string;
  actividades: ActividadEstudiante[];
  entregas: number;
}

/** Une los resultados de varias consultas en uno solo, estable entre renders. */
function combinar(resultados: UseQueryResult<NotasActividades>[]) {
  return {
    data: resultados.every((r) => r.data) ? resultados.map((r) => r.data!) : undefined,
    isLoading: resultados.some((r) => r.isLoading),
    error: resultados.find((r) => r.error)?.error ?? null,
    refetch: () => Promise.all(resultados.map((r) => r.refetch())),
  };
}

/**
 * Notas del estudiante en sesión, desde el backend:
 * matriz (GET /mis-notas) + notas por actividad de cada curso + entregas propias.
 * Los cortes y la definitiva parcial son los del backend; no se recalculan aquí.
 */
export function useMisNotas() {
  const { api, hoy, aprobatoria } = useApi();
  const u = useUsuario();
  const matriz = useQuery({ queryKey: qk.matriz, queryFn: () => api.estudiante.matriz() });
  const entregas = useQuery({ queryKey: qk.misEntregas, queryFn: () => api.estudiante.misEntregas() });
  const notas = useQueries({
    queries: (matriz.data?.cursos ?? []).map((c) => ({ queryKey: qk.notasCurso(c.cursoId), queryFn: () => api.estudiante.notasCurso(c.cursoId) })),
    combine: combinar,
  });

  const data = useMemo<NotasCurso[] | undefined>(() => {
    if (!matriz.data || !entregas.data || !notas.data) return undefined;
    const h = hoy();
    return matriz.data.cursos.map((cm, i) => {
      const curso = cursoDesdeMatriz(cm, i);
      const resumen = resumenDesdeMatriz(cm, aprobatoria);
      const actividades = (notas.data!.find((n) => n.cursoId === cm.cursoId)?.actividades ?? []).map((n): ActividadEstudiante => {
        const actividad = actividadDesdeNota(cm.cursoId, n);
        const entrega = entregas.data.find((e) => e.actividadId === n.actividadId);
        const calificacion = calificacionDesde(n, entrega, u.id);
        return { actividad, calificacion, entrega, estado: estadoEstudiante(actividad, calificacion, h) };
      });
      return { curso, resumen, mensaje: describirNecesidad(resumen.necesidad, aprobatoria), actividades, entregas: actividades.filter((a) => a.calificacion?.entregado).length };
    });
  }, [matriz.data, entregas.data, notas.data, hoy, aprobatoria, u.id]);

  const creditos = data?.map((n) => n.curso.creditos).filter((c): c is number => c != null) ?? [];
  return {
    data,
    /** null mientras el backend no exponga los créditos del curso. */
    totalCreditos: creditos.length ? creditos.reduce((a, c) => a + c, 0) : null,
    isLoading: matriz.isLoading || entregas.isLoading || notas.isLoading,
    error: matriz.error ?? entregas.error ?? notas.error,
    refetch: () => Promise.all([matriz.refetch(), entregas.refetch(), notas.refetch()]),
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
