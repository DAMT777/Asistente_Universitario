import { z } from 'zod';
import type { ArchivoEntrega } from '@/types';
import {
  actividadSchema, calificacionSchema, cursoSchema, entregaSchema, matrizNotasSchema, notasActividadesSchema, sesionSchema, usuarioSchema,
} from '@/schemas';
import { ApiError, type ApiClient } from './contratos';

export interface HttpConfig {
  /** URL del API Gateway, o "/api" con el proxy de Vite en desarrollo. */
  baseUrl: string;
  getToken: () => string | null | Promise<string | null>;
  /** Inyectable para tests o para un fetch con certificados en móvil. */
  fetchImpl?: typeof fetch;
}

/** Cuerpo de error estándar del backend: { status, codigo, mensaje, traceId }. */
const errorSchema = z.object({ codigo: z.string().optional(), mensaje: z.string().optional() });

export function createHttpApi(cfg: HttpConfig): ApiClient {
  const f = cfg.fetchImpl ?? fetch;

  async function req<T>(method: string, path: string, schema: z.ZodType<T, z.ZodTypeDef, unknown>, body?: unknown): Promise<T> {
    const token = await cfg.getToken();
    const isForm = typeof FormData !== 'undefined' && body instanceof FormData;
    let res: Response;
    try {
      res = await f(cfg.baseUrl + path, {
        method,
        headers: {
          Accept: 'application/json',
          ...(isForm || body === undefined ? {} : { 'Content-Type': 'application/json' }),
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: body === undefined ? undefined : isForm ? (body as FormData) : JSON.stringify(body),
      });
    } catch {
      throw new ApiError(0, 'No hay conexión con el servidor.');
    }
    if (!res.ok) {
      const err = errorSchema.safeParse(await res.json().catch(() => null));
      const datos = err.success ? err.data : {};
      throw new ApiError(res.status, datos.mensaje ?? 'No se pudo completar la solicitud.', datos.codigo);
    }
    if (res.status === 204) return schema.parse(undefined);
    return schema.parse(await res.json());
  }

  /** multipart/form-data con el campo "archivo". En React Native `datos` es { uri, name, type }. */
  function formulario(archivo: ArchivoEntrega): FormData {
    const fd = new FormData();
    fd.append('archivo', archivo.datos as Blob, archivo.nombre);
    return fd;
  }

  const vacio = z.any().transform(() => undefined as void);

  return {
    auth: {
      login: (input) => req('POST', '/auth/login', sesionSchema, input),
      logout: () => req('POST', '/auth/logout', vacio),
    },
    // Rol docente: estas rutas aún no existen en el backend (solo en la api simulada).
    cursos: {
      listar: () => req('GET', '/cursos', z.array(cursoSchema)),
      estudiantes: (id) => req('GET', `/cursos/${id}/estudiantes`, z.array(usuarioSchema)),
      actualizarPesos: (id, input) => req('PUT', `/cursos/${id}/pesos`, vacio, input),
    },
    actividades: {
      listar: (cursoId) => req('GET', cursoId ? `/cursos/${cursoId}/actividades` : '/actividades', z.array(actividadSchema)),
      crear: (input) => req('POST', `/cursos/${input.cursoId}/actividades`, actividadSchema, input),
    },
    calificaciones: {
      porActividad: (id) => req('GET', `/actividades/${id}/calificaciones`, z.array(calificacionSchema)),
      porCurso: (id) => req('GET', `/cursos/${id}/calificaciones`, z.array(calificacionSchema)),
      guardar: (input) => req('PUT', `/actividades/${input.actividadId}/calificaciones/${input.estudianteId}`, calificacionSchema, input),
      publicarBorradores: (id) => req('POST', `/actividades/${id}/publicar`, z.object({ publicadas: z.number() }).transform((r) => r.publicadas)),
    },
    estudiante: {
      matriz: () => req('GET', '/mis-notas', matrizNotasSchema),
      notasCurso: (cursoId) => req('GET', `/mis-notas/cursos/${encodeURIComponent(cursoId)}/actividades`, notasActividadesSchema),
      misEntregas: () => req('GET', '/mis-entregas', z.array(entregaSchema)),
      subirEntrega: (actividadId, archivo) => req('POST', `/actividades/${encodeURIComponent(actividadId)}/entregas`, entregaSchema, formulario(archivo)),
      editarEntrega: (entregaId, archivo) => req('PUT', `/entregas/${encodeURIComponent(entregaId)}`, entregaSchema, formulario(archivo)),
      anularEntrega: (entregaId) => req('DELETE', `/entregas/${encodeURIComponent(entregaId)}`, entregaSchema),
    },
  };
}
