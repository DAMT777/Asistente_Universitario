import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ArchivoDescargado, ArchivoEntrega } from '@/types';
import { seleccionEntregaSchema } from '@/schemas';
import { puedeEntregar } from '@/domain';
import { useApi } from './ApiContext';
import { useMisNotas } from './useMisNotas';
import { qk } from './queryKeys';

const mensajeDe = (e: unknown, porDefecto: string) => (e instanceof Error ? e.message : porDefecto);

/** Revisa una selección de archivos con las mismas reglas que el backend. Devuelve el primer error, si hay. */
export function validarSeleccion(archivos: ArchivoEntrega[]): string | null {
  const v = seleccionEntregaSchema.safeParse(archivos);
  return v.success ? null : v.error.issues[0].message;
}

/** CU-13 y CU-14: entregar uno o varios archivos, reemplazarlos, anular y descargar lo entregado. */
export function useEntrega(actividadId: string | undefined) {
  const { api, hoy } = useApi();
  const qc = useQueryClient();
  const notas = useMisNotas();
  const item = notas.data?.flatMap((n) => n.actividades.map((a) => ({ ...a, curso: n.curso }))).find((a) => a.actividad.id === actividadId);
  const vigente = item?.entrega?.estado === 'ENVIADA' ? item.entrega : undefined;

  const invalidar = () => qc.invalidateQueries({ queryKey: qk.misEntregas });
  const enviarMut = useMutation({
    // Con entrega vigente se reemplaza (PUT); si no hay o está anulada, se sube (POST reutiliza la fila).
    mutationFn: (archivos: ArchivoEntrega[]) => (vigente ? api.estudiante.editarEntrega(vigente.id, archivos) : api.estudiante.subirEntrega(actividadId!, archivos)),
    onSuccess: invalidar,
  });
  const anularMut = useMutation({ mutationFn: (entregaId: string) => api.estudiante.anularEntrega(entregaId), onSuccess: invalidar });

  // La fecha se compara por día; el backend aplica la hora exacta (RN-05) y responde FECHA_LIMITE_VENCIDA si ya pasó.
  const abierta = item ? puedeEntregar(item.actividad.vence, hoy(), item.estado === 'calificada') : false;

  /** Valida la selección y la envía. Devuelve el mensaje de error, si hay. */
  async function entregar(archivos: ArchivoEntrega[]): Promise<string | null> {
    const error = validarSeleccion(archivos);
    if (error) return error;
    try {
      await enviarMut.mutateAsync(archivos);
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

  /** Descarga un archivo de la entrega propia con su nombre original. */
  async function descargar(archivoId: string): Promise<{ archivo: ArchivoDescargado } | { error: string }> {
    const entrega = item?.entrega;
    const conocido = entrega?.archivos.find((a) => a.id === archivoId);
    if (!entrega || !conocido) return { error: 'El archivo no existe.' };
    try {
      const r = await api.estudiante.descargarArchivo(entrega.id, archivoId);
      return { archivo: { nombre: conocido.nombreArchivo, datos: r.datos } };
    } catch (e) {
      return { error: mensajeDe(e, 'No se pudo descargar el archivo.') };
    }
  }

  return {
    ...item,
    abierta,
    puedeAnular: abierta && !!vigente,
    entregar,
    anular,
    descargar,
    enviando: enviarMut.isPending || anularMut.isPending,
    isLoading: notas.isLoading,
    error: notas.error,
  };
}
