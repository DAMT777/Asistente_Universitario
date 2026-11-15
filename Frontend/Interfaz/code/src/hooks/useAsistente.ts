import { useCallback, useEffect, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { ApiError, type AccionPendiente } from '@/api';
import { useApi } from './ApiContext';
import { useUsuario } from './useSesion';

export type EstadoAccion = 'pendiente' | 'confirmando' | 'confirmada' | 'cancelada' | 'expirada' | 'fallida';

export interface MensajeChat {
  id: string;
  autor: 'usuario' | 'asistente';
  texto: string;
  /** Mensaje de error (no lo escribió el modelo). */
  error?: boolean;
  accion?: AccionPendiente & { estado: EstadoAccion; detalle?: string };
  fecha: string;
}

const MAX_MENSAJES = 60;
const clave = (usuarioId: string) => `aula.asistente.${usuarioId}`;

function leer(usuarioId: string): { conversacionId?: string; mensajes: MensajeChat[] } {
  try {
    const v = JSON.parse(sessionStorage.getItem(clave(usuarioId)) ?? 'null');
    if (v && Array.isArray(v.mensajes)) return v;
  } catch { /* almacenamiento no disponible */ }
  return { mensajes: [] };
}

/** Texto para el usuario según el código estable del contrato (contracts/asistente.yaml). */
export function mensajeDeError(e: unknown): string {
  if (!(e instanceof ApiError)) return 'No se pudo contactar al asistente. Revisa tu conexión e intenta de nuevo.';
  switch (e.codigo) {
    case 'LIMITE_MENSAJES': return 'Enviaste muchos mensajes seguidos. Espera un minuto e intenta de nuevo.';
    case 'ASISTENTE_NO_DISPONIBLE': return 'El asistente no está disponible en este momento. Intenta de nuevo en unos minutos.';
    case 'ACCION_EXPIRADA': return 'La propuesta venció. Pide de nuevo la calificación.';
    case 'ACCION_YA_USADA': return 'Esta propuesta ya fue confirmada o cancelada.';
    case 'ACCION_NO_ENCONTRADA': return 'La propuesta no existe o no es tuya.';
    case 'CONFLICTO_CONCURRENCIA': return 'Otra persona cambió esta nota mientras tanto. No se guardó; pide la calificación de nuevo.';
    default: return e.message;
  }
}

/**
 * Conversación con el asistente. Se guarda en sessionStorage por usuario (sobrevive a la navegación,
 * se borra al cerrar la pestaña) y nunca guarda tokens.
 */
export function useAsistente() {
  const { api } = useApi();
  const usuario = useUsuario();
  const qc = useQueryClient();
  const inicial = useRef(leer(usuario.id));
  const [conversacionId, setConversacionId] = useState<string | undefined>(inicial.current.conversacionId);
  const [mensajes, setMensajes] = useState<MensajeChat[]>(inicial.current.mensajes);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    try { sessionStorage.setItem(clave(usuario.id), JSON.stringify({ conversacionId, mensajes: mensajes.slice(-MAX_MENSAJES) })); }
    catch { /* sin almacenamiento: la conversación vive solo en memoria */ }
  }, [usuario.id, conversacionId, mensajes]);

  const agregar = (m: Omit<MensajeChat, 'id' | 'fecha'>) =>
    setMensajes(ms => [...ms, { ...m, id: crypto.randomUUID(), fecha: new Date().toISOString() }]);

  const actualizarAccion = (accionId: string, cambios: Partial<NonNullable<MensajeChat['accion']>>) =>
    setMensajes(ms => ms.map(m => m.accion?.id === accionId ? { ...m, accion: { ...m.accion, ...cambios } } : m));

  const enviar = useCallback(async (texto: string) => {
    const mensaje = texto.trim();
    if (!mensaje || enviando) return;
    agregar({ autor: 'usuario', texto: mensaje });
    setEnviando(true);
    try {
      const r = await api.asistente.enviar({ mensaje, conversacionId });
      setConversacionId(r.conversacionId);
      // El servicio agrega al final un resumen de la propuesta; la tarjeta ya lo muestra.
      const respuesta = r.accionPendiente ? r.respuesta.replace(/\n*Propuesta pendiente de confirmación:.*$/s, '').trim() : r.respuesta;
      agregar({ autor: 'asistente', texto: respuesta, accion: r.accionPendiente ? { ...r.accionPendiente, estado: 'pendiente' } : undefined });
    } catch (e) {
      agregar({ autor: 'asistente', texto: mensajeDeError(e), error: true });
    } finally {
      setEnviando(false);
    }
  }, [api, conversacionId, enviando]);

  const confirmar = useCallback(async (accionId: string) => {
    actualizarAccion(accionId, { estado: 'confirmando', detalle: undefined });
    try {
      const r = await api.asistente.confirmar(accionId);
      actualizarAccion(accionId, { estado: 'confirmada', detalle: `Nota ${r.valor.toFixed(1)} guardada en borrador. Publícala desde Calificar cuando quieras que el estudiante la vea.` });
      // Las pantallas de calificaciones y ponderado deben mostrar el cambio.
      await qc.invalidateQueries();
      return true;
    } catch (e) {
      const codigo = e instanceof ApiError ? e.codigo : undefined;
      actualizarAccion(accionId, { estado: codigo === 'ACCION_EXPIRADA' ? 'expirada' : codigo === 'ACCION_YA_USADA' ? 'cancelada' : 'fallida', detalle: mensajeDeError(e) });
      return false;
    }
  }, [api, qc]);

  const cancelar = useCallback(async (accionId: string) => {
    try {
      await api.asistente.cancelar(accionId);
      actualizarAccion(accionId, { estado: 'cancelada', detalle: 'Cancelaste la propuesta. No se guardó nada.' });
    } catch (e) {
      const codigo = e instanceof ApiError ? e.codigo : undefined;
      actualizarAccion(accionId, { estado: codigo === 'ACCION_EXPIRADA' ? 'expirada' : 'cancelada', detalle: mensajeDeError(e) });
    }
  }, [api]);

  const marcarExpirada = useCallback((accionId: string) =>
    actualizarAccion(accionId, { estado: 'expirada', detalle: 'La propuesta venció sin confirmarse. No se guardó nada.' }), []);

  const reiniciar = useCallback(() => { setMensajes([]); setConversacionId(undefined); }, []);

  return { mensajes, enviando, enviar, confirmar, cancelar, marcarExpirada, reiniciar };
}
