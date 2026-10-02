import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ArchivoEntrega } from '@/types';
import { archivoEntregaSchema, erroresPorCampo } from '@/schemas';
import { puedeEntregar } from '@/domain';
import { useApi } from './ApiContext';
import { useMisNotas } from './useMisNotas';
import { qk } from './queryKeys';

export function useEntrega(actividadId: string | undefined) {
  const { api, hoy } = useApi();
  const qc = useQueryClient();
  const notas = useMisNotas();
  const item = notas.data?.flatMap((n) => n.actividades.map((a) => ({ ...a, curso: n.curso }))).find((a) => a.actividad.id === actividadId);

  const mut = useMutation({
    mutationFn: (archivo: ArchivoEntrega) => api.calificaciones.entregar(actividadId!, archivo),
    onSuccess: () => qc.invalidateQueries({ queryKey: qk.califs }),
  });

  const anularMut = useMutation({ mutationFn: () => api.calificaciones.anularEntrega(actividadId!), onSuccess: () => qc.invalidateQueries({ queryKey: qk.califs }) });
  const abierta = item ? puedeEntregar(item.actividad.vence, hoy(), item.actividad.requiereEntrega) : false;

  /** Valida (formato y tamaño) y envía. Devuelve el mensaje de error, si hay. */
  async function entregar(archivo: ArchivoEntrega): Promise<string | null> {
    const v = archivoEntregaSchema.safeParse(archivo);
    if (!v.success) return Object.values(erroresPorCampo(v.error))[0];
    try {
      await mut.mutateAsync(archivo);
      return null;
    } catch (e) {
      return e instanceof Error ? e.message : 'No se pudo enviar la entrega.';
    }
  }

  async function anular() {
    try { await anularMut.mutateAsync(); return null; } catch(e) { return e instanceof Error ? e.message : 'No se pudo anular la entrega.'; }
  }
  return { ...item, anular, abierta, entregar, enviando: mut.isPending || anularMut.isPending, isLoading: notas.isLoading, error: notas.error };
}
