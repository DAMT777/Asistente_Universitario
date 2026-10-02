import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ArchivoEntrega } from '@/types';
import { archivoEntregaSchema, erroresPorCampo } from '@/schemas';
import { puedeEntregar } from '@/domain';
import { useApi } from './ApiContext';
import { useMisNotas } from './useMisNotas';
import { qk } from './queryKeys';

const mensajeDe = (e: unknown, porDefecto: string) => (e instanceof Error ? e.message : porDefecto);

/** CU-13 y CU-14: subir, reemplazar o anular la entrega propia de una actividad. */
export function useEntrega(actividadId: string | undefined) {
  const { api, hoy } = useApi();
  const qc = useQueryClient();
  const notas = useMisNotas();
  const item = notas.data?.flatMap((n) => n.actividades.map((a) => ({ ...a, curso: n.curso }))).find((a) => a.actividad.id === actividadId);
  const vigente = item?.entrega?.estado === 'ENVIADA' ? item.entrega : undefined;

  const invalidar = () => qc.invalidateQueries({ queryKey: qk.misEntregas });
  const enviarMut = useMutation({
    // Con entrega vigente se edita (PUT); si no hay o está anulada, se sube (POST reutiliza la fila).
    mutationFn: (archivo: ArchivoEntrega) => (vigente ? api.estudiante.editarEntrega(vigente.id, archivo) : api.estudiante.subirEntrega(actividadId!, archivo)),
    onSuccess: invalidar,
  });
  const anularMut = useMutation({ mutationFn: (entregaId: string) => api.estudiante.anularEntrega(entregaId), onSuccess: invalidar });

  // La fecha se compara por día; el backend aplica la hora exacta (RN-05) y responde FECHA_LIMITE_VENCIDA si ya pasó.
  const abierta = item ? puedeEntregar(item.actividad.vence, hoy(), item.estado === 'calificada') : false;

  /** Valida (formato y tamaño) y envía. Devuelve el mensaje de error, si hay. */
  async function entregar(archivo: ArchivoEntrega): Promise<string | null> {
    const v = archivoEntregaSchema.safeParse(archivo);
    if (!v.success) return Object.values(erroresPorCampo(v.error))[0];
    try {
      await enviarMut.mutateAsync(archivo);
      return null;
    } catch (e) {
      return mensajeDe(e, 'No se pudo enviar la entrega.');
    }
  }

  async function anular(): Promise<string | null> {
    if (!vigente) return 'No hay una entrega para anular.';
    try {
      await anularMut.mutateAsync(vigente.id);
      return null;
    } catch (e) {
      return mensajeDe(e, 'No se pudo anular la entrega.');
    }
  }

  return {
    ...item,
    abierta,
    puedeAnular: abierta && !!vigente,
    entregar,
    anular,
    enviando: enviarMut.isPending || anularMut.isPending,
    isLoading: notas.isLoading,
    error: notas.error,
  };
}
