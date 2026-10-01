import { useMemo } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { Calificacion, EstadoDocente, Usuario } from '@/types';
import { contarActividad, estadoDocente, type ConteoActividad } from '@/domain';
import { calificacionInputSchema, erroresPorCampo, type CalificacionInput } from '@/schemas';
import { useApi } from './ApiContext';
import { useActividades, useEstudiantes } from './useCursos';
import { qk } from './queryKeys';

export interface FilaCalificacion {
  estudiante: Usuario;
  calificacion: Calificacion | undefined;
  estado: EstadoDocente;
}

/** Calificaciones de un curso, por actividad, para el docente. */
export function useCalificaciones(cursoId: string | undefined, actividadId: string | undefined) {
  const { api, hoy } = useApi();
  const qc = useQueryClient();
  const acts = useActividades(cursoId);
  const ests = useEstudiantes(cursoId);
  const califs = useQuery({ queryKey: qk.califsCurso(cursoId ?? ''), queryFn: () => api.calificaciones.porCurso(cursoId!), enabled: !!cursoId });

  const actividad = acts.data?.find((a) => a.id === actividadId) ?? acts.data?.[0];

  const filas = useMemo<FilaCalificacion[] | undefined>(() => {
    if (!actividad || !ests.data || !califs.data) return undefined;
    return ests.data.map((estudiante) => {
      const calificacion = califs.data.find((c) => c.actividadId === actividad.id && c.estudianteId === estudiante.id);
      return { estudiante, calificacion, estado: estadoDocente(actividad, calificacion, hoy()) };
    });
  }, [actividad, ests.data, califs.data, hoy]);

  const conteos = useMemo(() => {
    const m = new Map<string, ConteoActividad>();
    if (acts.data && ests.data && califs.data) {
      const ids = ests.data.map((e) => e.id);
      for (const a of acts.data) m.set(a.id, contarActividad(a, califs.data, ids, hoy()));
    }
    return m;
  }, [acts.data, ests.data, califs.data, hoy]);

  const invalidar = () => qc.invalidateQueries({ queryKey: qk.califs });
  const guardarMut = useMutation({ mutationFn: api.calificaciones.guardar, onSuccess: invalidar });
  const publicarMut = useMutation({ mutationFn: api.calificaciones.publicarBorradores, onSuccess: invalidar });

  /** Valida con Zod y guarda como borrador o publicada. */
  async function guardar(input: CalificacionInput): Promise<Record<string, string> | null> {
    const v = calificacionInputSchema.safeParse(input);
    if (!v.success) return erroresPorCampo(v.error);
    await guardarMut.mutateAsync(v.data);
    return null;
  }

  return {
    actividades: acts.data,
    actividad,
    filas,
    conteos,
    conteo: actividad ? conteos.get(actividad.id) : undefined,
    totalEstudiantes: ests.data?.length ?? 0,
    guardar,
    publicarBorradores: () => (actividad ? publicarMut.mutateAsync(actividad.id) : Promise.resolve(0)),
    guardando: guardarMut.isPending || publicarMut.isPending,
    isLoading: acts.isLoading || ests.isLoading || califs.isLoading,
    error: acts.error ?? ests.error ?? califs.error,
  };
}
