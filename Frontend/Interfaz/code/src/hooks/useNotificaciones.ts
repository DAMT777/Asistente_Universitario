import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { BandejaNotificaciones } from '@/types';
import { useApi } from './ApiContext';
import { useUsuario } from './useSesion';

/** Cada cuánto se consulta si llegó algo nuevo mientras la aplicación está abierta. */
export const INTERVALO_NOTIFICACIONES_MS = 30_000;

/**
 * Bandeja de notificaciones del usuario de la sesión. Se refresca sola cada 30 segundos y al volver a la pestaña.
 * Marcar como leída actualiza la vista al instante y luego confirma con el servicio.
 */
export function useNotificaciones() {
  const { api } = useApi();
  const usuario = useUsuario();
  const qc = useQueryClient();
  const clave = ['notificaciones', usuario.id] as const;

  const consulta = useQuery({
    queryKey: clave,
    queryFn: () => api.notificaciones.listar(),
    refetchInterval: INTERVALO_NOTIFICACIONES_MS,
    refetchOnWindowFocus: true,
    staleTime: 10_000,
  });

  /** Aplica el cambio en caché y devuelve cómo deshacerlo si el servicio lo rechaza. */
  const optimista = async (cambiar: (b: BandejaNotificaciones) => BandejaNotificaciones) => {
    await qc.cancelQueries({ queryKey: clave });
    const anterior = qc.getQueryData<BandejaNotificaciones>(clave);
    if (anterior) qc.setQueryData(clave, cambiar(anterior));
    return { anterior };
  };
  const revertir = (_: unknown, __: unknown, ctx?: { anterior?: BandejaNotificaciones }) => { if (ctx?.anterior) qc.setQueryData(clave, ctx.anterior); };
  const refrescar = () => qc.invalidateQueries({ queryKey: clave });

  const marcarLeida = useMutation({
    mutationFn: (id: string) => api.notificaciones.marcarLeida(id),
    onMutate: (id) => optimista(b => {
      const n = b.notificaciones.find(x => x.id === id);
      if (!n || n.leida) return b;
      return { noLeidas: Math.max(0, b.noLeidas - 1), notificaciones: b.notificaciones.map(x => x.id === id ? { ...x, leida: true } : x) };
    }),
    onError: revertir,
    onSettled: refrescar,
  });

  const marcarTodas = useMutation({
    mutationFn: () => api.notificaciones.marcarTodasLeidas(),
    onMutate: () => optimista(b => ({ noLeidas: 0, notificaciones: b.notificaciones.map(x => ({ ...x, leida: true })) })),
    onError: revertir,
    onSettled: refrescar,
  });

  return {
    ...consulta,
    noLeidas: consulta.data?.noLeidas ?? 0,
    notificaciones: consulta.data?.notificaciones ?? [],
    marcarLeida: (id: string) => marcarLeida.mutate(id),
    marcarTodas: () => marcarTodas.mutate(),
    marcandoTodas: marcarTodas.isPending,
  };
}
