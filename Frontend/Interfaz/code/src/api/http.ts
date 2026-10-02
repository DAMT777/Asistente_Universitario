import { z } from 'zod';
import { actividadSchema, calificacionSchema, cursoSchema, usuarioSchema } from '@/schemas';
import type { Rol } from '@/types';
import { ApiError, type ApiClient } from './contratos';

/** Respuesta real del Servicio de Usuarios (POST /api/auth/login). */
const authRespuestaSchema = z.object({
  accessToken: z.string(),
  userId: z.string(),
  username: z.string(),
  fullName: z.string(),
  roles: z.array(z.string()),
});

export interface HttpConfig {
  baseUrl: string;
  getToken: () => string | null | Promise<string | null>;
  /** Inyectable para tests o para un fetch con certificados en móvil. */
  fetchImpl?: typeof fetch;
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
      const msg = await res.json().then((j: { mensaje?: string; error?: string }) => j.mensaje ?? j.error).catch(() => undefined);
      throw new ApiError(res.status, msg ?? 'No se pudo completar la solicitud.');
    }
    if (res.status === 204) return schema.parse(undefined);
    return schema.parse(await res.json());
  }

  const vacio = z.any().transform(() => undefined as void);

  return {
    auth: {
      // El backend usa { usernameOrEmail, password } y devuelve { accessToken, ... }.
      // Aquí se traduce al contrato { token, usuario } que usa el resto del frontend.
      login: async ({ usuario, contrasena, rol }) => {
        const r = await req('POST', '/auth/login', authRespuestaSchema, { usernameOrEmail: usuario, password: contrasena })
          .catch((e) => {
            if (e instanceof ApiError && e.status === 401) throw new ApiError(401, 'Usuario o contraseña incorrectos.');
            throw e;
          });
        const rolReal: Rol = r.roles.includes('PROFESOR') ? 'docente' : 'estudiante';
        if (rolReal !== rol) throw new ApiError(403, `Esta cuenta no es de ${rol}.`);
        return { token: r.accessToken, usuario: { id: r.userId, nombre: r.fullName, codigo: r.username, rol: rolReal } };
      },
      // Con JWT no hay endpoint de logout: basta con descartar el token en el cliente.
      logout: async () => undefined,
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
      porActividad: (id) => req('GET', `/actividades/${id}/calificaciones`, z.array(calificacionSchema)),
      porCurso: (id) => req('GET', `/cursos/${id}/calificaciones`, z.array(calificacionSchema)),
      delEstudiante: (id) => req('GET', `/estudiantes/${id}/calificaciones`, z.array(calificacionSchema)),
      guardar: (input) => req('PUT', `/actividades/${input.actividadId}/calificaciones/${input.estudianteId}`, calificacionSchema, input),
      publicarBorradores: (id) => req('POST', `/actividades/${id}/publicar`, z.object({ publicadas: z.number() }).transform((r) => r.publicadas)),
      entregar: (id, archivo) => {
        const fd = new FormData();
        // En web `datos` es un Blob; en React Native es { uri, name, type }.
        fd.append('archivo', archivo.datos as Blob, archivo.nombre);
        return req('POST', `/actividades/${id}/entregas`, calificacionSchema, fd);
      },
    },
  };
}
