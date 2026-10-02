import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ArchivoEntrega } from '@/types';
import { seleccionEntregaSchema } from '@/schemas';
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
    mutationFn: (archivos: ArchivoEntrega[]) => api.calificaciones.entregar(actividadId!, archivos),
    onSuccess: () => qc.invalidateQueries({ queryKey: qk.califs }),
  });

  const anularMut = useMutation({ mutationFn: () => api.calificaciones.anularEntrega(actividadId!), onSuccess: () => qc.invalidateQueries({ queryKey: qk.califs }) });
  const abierta = item ? puedeEntregar(item.actividad.vence, hoy(), item.actividad.requiereEntrega) : false;

  /** Valida la selección (formato, tamaño por archivo, total y cantidad) y la envía. Devuelve el mensaje de error, si hay. */
  async function entregar(archivos: ArchivoEntrega[]): Promise<string | null> {
    const v = seleccionEntregaSchema.safeParse(archivos);
    if (!v.success) return v.error.issues[0].message;
    try {
      await mut.mutateAsync(archivos);
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
