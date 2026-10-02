import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { Corte } from '@/types';
import { resumenCurso, validarPesos } from '@/domain';
import { useApi } from './ApiContext';
import { useActividades, useCurso, useEstudiantes } from './useCursos';
import { qk } from './queryKeys';

interface Borrador {
  cortes: [string, string, string];
  actividades: Record<string, string>;
}

/** Edición de pesos con validación en vivo y acumulado por estudiante. */
export function usePonderado(cursoId: string | undefined) {
  const { api, aprobatoria } = useApi();
  const qc = useQueryClient();
  const cursoQ = useCurso(cursoId);
  const curso = cursoQ.data;
  const actsQ = useActividades(cursoId);
  const acts = actsQ.data;
  const estsQ = useEstudiantes(cursoId);
  const ests = estsQ.data;
  const califsQ = useQuery({ queryKey: qk.califsCurso(cursoId ?? ''), queryFn: () => api.calificaciones.porCurso(cursoId!), enabled: !!cursoId });
  const califs = califsQ.data;

  const [borrador, setBorrador] = useState<Borrador | null>(null);
  useEffect(() => {
    if (curso && acts) setBorrador({ cortes: curso.pesos.map(String) as Borrador['cortes'], actividades: Object.fromEntries(acts.map((a) => [a.id, String(a.peso)])) });
  }, [curso, acts]);

  const validacion = useMemo(() => {
    if (!borrador || !acts) return null;
    const cortes = validarPesos(borrador.cortes);
    const grupos = ([1, 2, 3] as Corte[])
      .map((k) => ({ corte: k, actividades: acts.filter((a) => a.corte === k) }))
      .filter((g) => g.actividades.length)
      .map((g) => ({ ...g, ...validarPesos(g.actividades.map((a) => borrador.actividades[a.id]), false) }));
    return { cortes, grupos, ok: cortes.ok && grupos.every((g) => g.ok) };
  }, [borrador, acts]);

  const resumenEstudiantes = useMemo(() => {
    if (!curso || !acts || !ests || !califs) return undefined;
    return ests.map((e) => ({ estudiante: e, resumen: resumenCurso(curso, acts, califs.filter((c) => c.estudianteId === e.id), aprobatoria) }));
  }, [curso, acts, ests, califs, aprobatoria]);

  const mut = useMutation({
    mutationFn: () => api.cursos.actualizarPesos(cursoId!, {
      cortes: borrador!.cortes.map(Number) as [number, number, number],
      actividades: Object.fromEntries(Object.entries(borrador!.actividades).map(([k, v]) => [k, Number(v)])),
    }),
    onSettled: () => Promise.all([qc.invalidateQueries({ queryKey: qk.cursos }), qc.invalidateQueries({ queryKey: ['actividades'] })]),
  });

  const soloDigitos = (v: string) => v.replace(/[^\d]/g, '');
  return {
    error: cursoQ.error ?? actsQ.error ?? estsQ.error ?? califsQ.error ?? mut.error,
    refetch: () => { mut.reset(); return Promise.all([cursoQ.refetch(), actsQ.refetch(), estsQ.refetch(), califsQ.refetch()]); },
    curso,
    actividades: acts,
    borrador,
    validacion,
    resumenEstudiantes,
    setPesoCorte: (i: 0 | 1 | 2, v: string) => setBorrador((b) => b && { ...b, cortes: b.cortes.map((x, j) => (j === i ? soloDigitos(v) : x)) as Borrador['cortes'] }),
    setPesoActividad: (id: string, v: string) => setBorrador((b) => b && { ...b, actividades: { ...b.actividades, [id]: soloDigitos(v) } }),
    guardar: async () => {
      if (!validacion?.ok) return false;
      try { await mut.mutateAsync(); return true; } catch { return false; }
    },
    guardando: mut.isPending,
  };
}
