import { useMemo } from 'react';
import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';
import { actividadInputSchema, erroresPorCampo } from '@/schemas';
import { contarActividad, validarPesos, fechaFormulario } from '@/domain';
import { useApi } from './ApiContext';
import { useActividades, useCursos } from './useCursos';
import { qk } from './queryKeys';

/** Actividades con entregas sin calificar o en borrador, en todos los cursos del docente. */
export function usePendientesDocente() {
  const { api, hoy } = useApi();
  const cursos = useCursos();
  const acts = useActividades();
  const ids = cursos.data?.map((c) => c.id) ?? [];
  const califs = useQueries({ queries: ids.map((id) => ({ queryKey: qk.califsCurso(id), queryFn: () => api.calificaciones.porCurso(id) })) });
  const ests = useQueries({ queries: ids.map((id) => ({ queryKey: qk.estudiantes(id), queryFn: () => api.cursos.estudiantes(id) })) });
  const listo = !!acts.data && califs.every((q) => q.data) && ests.every((q) => q.data);

  const data = useMemo(() => {
    if (!listo || !cursos.data) return undefined;
    return cursos.data.flatMap((curso, i) => {
      const estIds = ests[i].data!.map((e) => e.id);
      return acts.data!
        .filter((a) => a.cursoId === curso.id)
        .map((actividad) => ({ curso, actividad, conteo: contarActividad(actividad, califs[i].data!, estIds, hoy()) }))
        .filter((x) => x.conteo.sinCalificar + x.conteo.borradores > 0);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [listo, cursos.data, acts.data, ...califs.map((q) => q.data), ...ests.map((q) => q.data)]);

  const error = cursos.error ?? acts.error ?? califs.find(q => q.error)?.error ?? ests.find(q => q.error)?.error;
  return { data, error, refetch: () => Promise.all([cursos.refetch(), acts.refetch(), ...califs.map(q => q.refetch()), ...ests.map(q => q.refetch())]), totalSinCalificar: data?.reduce((a, x) => a + x.conteo.sinCalificar, 0) ?? 0, isLoading: !listo && !error };
}

export function useCrearActividad(cursoId: string) {
  const { api, hoy } = useApi();
  const qc = useQueryClient();
  const acts = useActividades(cursoId);
  const editarMut = useMutation({ mutationFn: ({ id, datos }: { id: string; datos: Parameters<typeof api.actividades.crear>[0] }) => api.actividades.editar(id, datos), onSuccess: () => qc.invalidateQueries({ queryKey: ['actividades'] }) });
  const mut = useMutation({ mutationFn: api.actividades.crear, onSuccess: () => qc.invalidateQueries({ queryKey: ['actividades'] }) });

  /** Devuelve errores por campo o, si se creó, el total de pesos que queda en el corte. */
  async function crear(input: { titulo: string; corte: string; peso: string; vence: string; requiereEntrega: boolean }, id?: string) {
    const v = actividadInputSchema(id ? fechaFormulario(input.vence) || hoy() : hoy()).safeParse({ ...input, vence: fechaFormulario(input.vence), cursoId });
    if (!v.success) return { ok: false as const, errores: erroresPorCampo(v.error) };
    const previos = (acts.data ?? []).filter(a => a.corte === v.data.corte && a.id !== id).reduce((sum, a) => sum + a.peso, 0);
    if (previos + v.data.peso > 100) return { ok: false as const, errores: { peso: `Solo quedan ${100 - previos}% disponibles en este corte.` } };
    try { if (id) await editarMut.mutateAsync({ id, datos: v.data }); else await mut.mutateAsync(v.data); } catch (e) { return { ok: false as const, errores: { _: e instanceof Error ? e.message : 'No se pudo crear la actividad.' } }; }
    const delCorte = (acts.data ?? []).filter((a) => a.corte === v.data.corte).map((a) => a.peso);
    const total = validarPesos([...delCorte, v.data.peso]).total;
    return { ok: true as const, corte: v.data.corte, total };
  }
  return { crear, creando: mut.isPending || editarMut.isPending };
}
