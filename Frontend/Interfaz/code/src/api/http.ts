import { z } from 'zod';
import type { Actividad, Calificacion, PublicacionCorte, Usuario } from '@/types';
import { actividadSchema, cursoSchema, usuarioSchema } from '@/schemas';
import { ApiError, type ApiClient } from './contratos';

export interface HttpConfig {
  baseUrl: string;
  getToken: () => string | null | Promise<string | null>;
  fetchImpl?: typeof fetch;
}

// DTOs externos de la guía técnica; los hooks conservan sus modelos de vista.
const usuarioDto = z.object({ id: z.string(), nombre: z.string(), codigo: z.string().optional(), codigoInstitucional: z.string().optional(), rol: z.enum(['PROFESOR', 'ESTUDIANTE']) }).transform(u => ({ id: u.id, nombre: u.nombre, codigo: u.codigo ?? u.codigoInstitucional ?? '', rol: u.rol === 'PROFESOR' ? 'docente' as const : 'estudiante' as const }));
const usuarioRespuesta = z.union([usuarioSchema, usuarioDto]);
const actividadDto = z.object({ id: z.string(), cursoId: z.string(), titulo: z.string(), corte: z.union([z.literal(1), z.literal(2), z.literal(3)]), peso: z.number(), fechaLimite: z.string().nullable(), requiereEntrega: z.boolean() }).transform(a => ({ ...a, vence: a.fechaLimite ?? '' }));
const actividadRespuesta = z.union([actividadSchema, actividadDto]);
const cursoDto = z.object({ id: z.string(), codigo: z.string(), nombre: z.string(), profesor: z.string().optional(), nombreProfesor: z.string().optional(), pesoCorte1: z.number(), pesoCorte2: z.number(), pesoCorte3: z.number(), periodo: z.string().default(''), creditos: z.number().default(0), grupo: z.number().default(1) }).transform(c => ({ ...c, docente: c.profesor ?? c.nombreProfesor ?? '', pesos: [c.pesoCorte1,c.pesoCorte2,c.pesoCorte3] as [number,number,number], monograma: c.nombre.split(' ').slice(0,2).map(x => x[0]).join(''), acento: 'rojo' as const }));
const cursoRespuesta = z.union([cursoSchema, cursoDto]);
const calificacionDto = z.object({ actividadId: z.string(), estudianteId: z.string().optional(), valor: z.number().nullable(), estado: z.enum(['BORRADOR', 'PUBLICADA']).nullable().optional(), retroalimentacion: z.string().default('') });
const entregaDto = z.object({ id: z.string(), actividadId: z.string(), estudianteId: z.string(), fechaEnvio: z.string(), estado: z.enum(['ENVIADA', 'ANULADA']), nombreArchivo: z.string(), tamano: z.number() });
const publicacionDto = z.object({ estudianteId: z.string(), corte: z.union([z.literal(1),z.literal(2),z.literal(3)]), nota: z.number(), fechaPublicacion: z.string().optional() });

export function createHttpApi(cfg: HttpConfig): ApiClient {
  const f = cfg.fetchImpl ?? fetch;
  let actual: Usuario | null = null;
  async function req<T>(method: string, path: string, schema: z.ZodType<T, z.ZodTypeDef, unknown>, body?: unknown): Promise<T> {
    const token = await cfg.getToken();
    const formulario = body instanceof FormData;
    const res = await f(cfg.baseUrl.replace(/\/$/, '') + path, { method, headers: { Accept: 'application/json', ...(formulario || body === undefined ? {} : { 'Content-Type': 'application/json' }), ...(token ? { Authorization: `Bearer ${token}` } : {}) }, body: body === undefined ? undefined : formulario ? body : JSON.stringify(body) });
    if (!res.ok) {
      const msg = await res.json().then((j: { mensaje?: string }) => j.mensaje).catch(() => undefined);
      throw new ApiError(res.status, msg ?? 'No se pudo completar la solicitud.');
    }
    if (res.status === 204) return schema.parse(undefined);
    try { return schema.parse(await res.json()); }
    catch { throw new ApiError(502, 'La respuesta del servicio no cumple el contrato esperado. Revisa la integración de la API.'); }
  }
  const vacio = z.unknown().transform(() => undefined as void);
  const identidad = async () => actual ?? (actual = await req('GET', '/auth/me', usuarioRespuesta));
  const cursos = () => req('GET', '/cursos', z.array(cursoRespuesta));
  const actividades = async (cursoId?: string): Promise<Actividad[]> => cursoId ? req('GET', `/cursos/${cursoId}/actividades`, z.array(actividadRespuesta)) : (await Promise.all((await cursos()).map(c => actividades(c.id)))).flat();
  const entregaPropia = (id: string) => req('GET', `/mis-entregas?actividadId=${encodeURIComponent(id)}`, z.array(entregaDto));
  const archivoUrl = async (id: string) => {
    const token = await cfg.getToken();
    const res = await f(cfg.baseUrl.replace(/\/$/, '') + `/entregas/${id}/archivo`, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
    if (!res.ok) throw new ApiError(res.status, 'No se pudo descargar el archivo de la entrega.');
    return URL.createObjectURL(await res.blob());
  };
  // No se descarga cada archivo al listar. La UI solicita la URL bajo demanda.
  function combinar(c: z.infer<typeof calificacionDto> | undefined, e: z.infer<typeof entregaDto> | undefined, actividadId: string, estudianteId: string): Calificacion {
    return { actividadId, estudianteId, nota: c?.valor ?? null, estado: c?.estado === 'BORRADOR' ? 'borrador' : c?.valor != null ? 'publicada' : null, retro: c?.retroalimentacion ?? '', entregado: e?.estado === 'ENVIADA' ? e.fechaEnvio : null, archivo: e?.estado === 'ENVIADA' ? e.nombreArchivo : null, tamano: e?.estado === 'ENVIADA' ? e.tamano : undefined, entregaId: e?.estado === 'ENVIADA' ? e.id : undefined };
  }
  const porActividad = async (id: string): Promise<Calificacion[]> => {
    const [notas, entregas] = await Promise.all([req('GET', `/actividades/${id}/calificaciones`, z.array(calificacionDto)), req('GET', `/actividades/${id}/entregas`, z.array(entregaDto))]);
    const ids = new Set([...notas.map(c => c.estudianteId!), ...entregas.map(e => e.estudianteId)]);
    return [...ids].map(e => combinar(notas.find(c => c.estudianteId === e), entregas.find(x => x.estudianteId === e && x.estado === 'ENVIADA'), id, e));
  };
  return {
    auth: {
      async login(input) {
        const dto = await req('POST', '/auth/login', z.object({ accessToken: z.string(), usuario: usuarioRespuesta }), { usuario: input.usuario, password: input.contrasena });
        actual = dto.usuario; return { token: dto.accessToken, usuario: dto.usuario };
      },
      async logout() { actual = null; },
    },
    cursos: {
      listar: cursos,
      estudiantes: id => req('GET', `/cursos/${id}/estudiantes`, z.array(usuarioRespuesta)),
      async actualizarPesos(id, input) {
        // El contrato separa pesos de cortes y edición de actividades.
        await req('PUT', `/cursos/${id}/pesos`, vacio, { pesoCorte1: input.cortes[0], pesoCorte2: input.cortes[1], pesoCorte3: input.cortes[2] });
        const acts = await actividades(id);
        const cambios = acts.filter(a => input.actividades[a.id] !== undefined && input.actividades[a.id] !== a.peso).sort((a,b) => (input.actividades[a.id] - a.peso) - (input.actividades[b.id] - b.peso));
        for (const a of cambios) await req('PUT', `/actividades/${a.id}`, actividadRespuesta, { titulo: a.titulo, corte: a.corte, peso: input.actividades[a.id], fechaLimite: a.vence || null, requiereEntrega: a.requiereEntrega });
      },
    },
    actividades: {
      listar: actividades,
      crear: a => req('POST', `/cursos/${a.cursoId}/actividades`, actividadRespuesta, { titulo: a.titulo, corte: a.corte, peso: a.peso, fechaLimite: a.vence || null, requiereEntrega: a.requiereEntrega }),
      editar: (id,a) => req('PUT', `/actividades/${id}`, actividadRespuesta, { titulo: a.titulo, corte: a.corte, peso: a.peso, fechaLimite: a.vence || null, requiereEntrega: a.requiereEntrega }),
    },
    cortes: {
      async listar(cursoId) {
        const u = await identidad();
        if (u.rol === 'estudiante') {
          const matriz = await req('GET', '/mis-notas', z.object({ cursos: z.array(z.object({ cursoId: z.string(), cortes: z.array(z.object({ corte: z.union([z.literal(1),z.literal(2),z.literal(3)]), nota: z.number().nullable(), publicado: z.boolean(), fechaPublicacion: z.string().optional() })) })) }));
          return matriz.cursos.filter(c => !cursoId || c.cursoId === cursoId).flatMap(c => c.cortes.filter(k => k.publicado && k.nota !== null).map(k => ({ cursoId: c.cursoId, estudianteId: u.id, corte: k.corte, nota: k.nota!, fechaPublicacion: k.fechaPublicacion ?? '' })));
        }
        // La guía no fija el JSON de ponderado: integración propuesta documentada en README.
        const ids = cursoId ? [cursoId] : (await cursos()).map(c => c.id);
        return (await Promise.all(ids.map(async id => {
          const dto = await req('GET', `/cursos/${id}/ponderado`, z.object({ publicaciones: z.array(publicacionDto) }));
          return dto.publicaciones.map(p => ({ ...p, cursoId: id, fechaPublicacion: p.fechaPublicacion ?? '' }));
        }))).flat();
      },
      async publicar(input): Promise<PublicacionCorte> {
        if (input.corregir) {
          const p = await req('PUT', `/cursos/${input.cursoId}/cortes/${input.corte}/estudiantes/${input.estudianteId}`, z.object({ nota: z.number(), fechaPublicacion: z.string().optional() }), { omitirBorradores: input.omitirBorradores });
          return { cursoId: input.cursoId, estudianteId: input.estudianteId, corte: input.corte, nota: p.nota, fechaPublicacion: p.fechaPublicacion ?? '' };
        }
        const r = await req('POST', `/cursos/${input.cursoId}/cortes/${input.corte}/publicar`, z.object({ publicados: z.array(z.object({ estudianteId: z.string(), nota: z.number(), fechaPublicacion: z.string().optional() })), rechazados: z.array(z.object({ estudianteId: z.string(), mensaje: z.string() })) }), { estudiantes: [input.estudianteId], omitirBorradores: input.omitirBorradores });
        const p = r.publicados.find(p => p.estudianteId === input.estudianteId);
        if (!p) throw new ApiError(422, r.rechazados.find(p => p.estudianteId === input.estudianteId)?.mensaje ?? 'El corte no se publicó para este estudiante.');
        return { cursoId: input.cursoId, estudianteId: input.estudianteId, corte: input.corte, nota: p.nota, fechaPublicacion: p.fechaPublicacion ?? '' };
      },
    },
    calificaciones: {
      porActividad,
      porCurso: async id => (await Promise.all((await actividades(id)).map(a => porActividad(a.id)))).flat(),
      async delEstudiante(id) {
        const u = await identidad(); if (u.id !== id) throw new ApiError(403, 'Solo puedes consultar tus propias notas.');
        const [cs, entregas] = await Promise.all([cursos(), req('GET', '/mis-entregas', z.array(entregaDto))]);
        const notas = (await Promise.all(cs.map(c => req('GET', `/mis-notas/cursos/${c.id}/actividades`, z.array(calificacionDto))))).flat();
        const ids = new Set([...notas.map(n => n.actividadId), ...entregas.map(e => e.actividadId)]);
        return [...ids].map(a => combinar(notas.find(n => n.actividadId === a), entregas.find(e => e.actividadId === a && e.estado === 'ENVIADA'), a,id));
      },
      async guardar(input) {
        if (input.publicar) throw new ApiError(422, 'Guarda el borrador y usa Publicar notas de actividad.');
        await req('PUT', `/actividades/${input.actividadId}/calificaciones/${input.estudianteId}`, calificacionDto, { valor: input.nota, retroalimentacion: input.retro });
        return { actividadId: input.actividadId, estudianteId: input.estudianteId, nota: input.nota, retro: input.retro, estado: 'borrador', entregado: null, archivo: null };
      },
      async publicarBorradores(id) {
        const antes = (await porActividad(id)).filter(c => c.estado === 'borrador').length;
        await req('POST', `/actividades/${id}/calificaciones/publicar`, vacio);
        return antes;
      },
      async anularEntrega(id) {
        const e = (await entregaPropia(id)).find(e => e.actividadId === id && e.estado === 'ENVIADA');
        if (!e) throw new ApiError(404, 'No hay una entrega activa.');
        await req('DELETE', `/entregas/${e.id}`, vacio);
      },
      async entregar(id, archivo) {
        const anterior = (await entregaPropia(id)).find(e => e.actividadId === id && e.estado === 'ENVIADA');
        const fd = new FormData(); fd.append('archivo', archivo.datos as Blob, archivo.nombre);
        const e = await req(anterior ? 'PUT' : 'POST', anterior ? `/entregas/${anterior.id}` : `/actividades/${id}/entregas`, entregaDto, fd);
        return combinar(undefined,e,id,e.estudianteId);
      },
      descargarArchivo: archivoUrl,
    },
  };
}
