import { z } from 'zod';
import { actividadSchema, calificacionApiSchema, calificacionSchema, cursoSchema, sesionSchema, usuarioSchema, type CalificacionApi } from '@/schemas';
import type { Calificacion } from '@/types';
import { ApiError, type ApiClient } from './contratos';

export interface HttpConfig {
  baseUrl: string;
  getToken: () => string | null | Promise<string | null>;
  /** Inyectable para tests o para un fetch con certificados en móvil. */
  fetchImpl?: typeof fetch;
}

/**
 * El servicio de evaluaciones habla en `valor`, `retroalimentacion` y BORRADOR/PUBLICADA. La entrega (archivo y fecha)
 * vive en el servicio de entregas, así que aquí queda vacía y la pantalla la combina aparte.
 */
function aCalificacion(c: CalificacionApi): Calificacion {
  return {
    actividadId: c.actividadId,
    estudianteId: c.estudianteId,
    entregado: null,
    archivo: null,
    nota: c.valor,
    estado: c.estado === 'PUBLICADA' ? 'publicada' : 'borrador',
    retro: c.retroalimentacion ?? '',
  };
}

export function createHttpApi(cfg: HttpConfig): ApiClient {
  const f = cfg.fetchImpl ?? fetch;

  async function req<T>(method: string, path: string, schema: z.ZodType<T, z.ZodTypeDef, unknown>, body?: unknown): Promise<T> {
    const token = await cfg.getToken();
    const isForm = typeof FormData !== 'undefined' && body instanceof FormData;
    const res = await f(cfg.baseUrl + path, {
      method,
      headers: {
        Accept: 'application/json',
        ...(isForm || body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body === undefined ? undefined : isForm ? (body as FormData) : JSON.stringify(body),
    });
    if (!res.ok) {
      const msg = await res.json().then((j: { mensaje?: string }) => j.mensaje).catch(() => undefined);
      throw new ApiError(res.status, msg ?? 'No se pudo completar la solicitud.');
    }
    if (res.status === 204) return schema.parse(undefined);
    return schema.parse(await res.json());
  }

  const vacio = z.any().transform(() => undefined as void);
  const publicadasSchema = z.object({ publicadas: z.number() }).transform((r) => r.publicadas);

  return {
    auth: {
      login: (input) => req('POST', '/auth/login', sesionSchema, input),
      logout: () => req('POST', '/auth/logout', vacio),
    },
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
      porActividad: (id) => req('GET', `/actividades/${id}/calificaciones`, z.array(calificacionApiSchema).transform((l) => l.map(aCalificacion))),
      porCurso: (id) => req('GET', `/cursos/${id}/calificaciones`, z.array(calificacionSchema)),
      delEstudiante: (id) => req('GET', `/estudiantes/${id}/calificaciones`, z.array(calificacionSchema)),
      // CU-05, CU-06 y CU-07: el PUT deja la nota en borrador. "Publicar" desde la tarjeta del estudiante
      // publica solo esa nota (POST publicar con estudianteIds).
      guardar: async (input) => {
        const dto = await req('PUT', `/actividades/${input.actividadId}/calificaciones/${input.estudianteId}`, calificacionApiSchema,
          { valor: input.nota, retroalimentacion: input.retro });
        if (!input.publicar) return aCalificacion(dto);
        await req('POST', `/actividades/${input.actividadId}/calificaciones/publicar`, publicadasSchema, { estudianteIds: [input.estudianteId] });
        return aCalificacion({ ...dto, estado: 'PUBLICADA' });
      },
      // CU-08
      publicarBorradores: (id) => req('POST', `/actividades/${id}/calificaciones/publicar`, publicadasSchema),
      entregar: (id, archivo) => {
        const fd = new FormData();
        // En web `datos` es un Blob; en React Native es { uri, name, type }.
        fd.append('archivo', archivo.datos as Blob, archivo.nombre);
        return req('POST', `/actividades/${id}/entregas`, calificacionSchema, fd);
      },
    },
  };
}
