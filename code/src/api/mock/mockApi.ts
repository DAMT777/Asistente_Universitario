import type { Calificacion, Sesion, Usuario } from '@/types';
import { ApiError, type ApiClient } from '../contratos';
import { ACTIVIDADES, CURSOS, DOCENTE, ESTUDIANTES, calificacionesIniciales } from './seed';

export interface MockConfig {
  hoy: () => string;
  latenciaMs?: number;
}

/** Implementación en memoria del contrato. Misma forma que el backend real. */
export function createMockApi(cfg: MockConfig): ApiClient {
  const db = {
    cursos: structuredClone(CURSOS),
    actividades: structuredClone(ACTIVIDADES),
    califs: calificacionesIniciales(),
  };
  let usuario: Usuario | null = null;
  const espera = <T>(v: T) => new Promise<T>((r) => setTimeout(() => r(structuredClone(v)), cfg.latenciaMs ?? 250));
  const buscar = (a: string, e: string) => db.califs.find((c) => c.actividadId === a && c.estudianteId === e);
  const exigirDocente = () => {
    if (usuario?.rol !== 'docente') throw new ApiError(403, 'Solo el docente puede hacer esto.');
  };

  return {
    auth: {
      async login({ usuario: u, rol }) {
        const encontrado = rol === 'docente' ? (u === DOCENTE.codigo ? DOCENTE : null) : ESTUDIANTES.find((e) => e.codigo === u) ?? null;
        if (!encontrado) throw new ApiError(401, 'Usuario o contraseña incorrectos.');
        usuario = encontrado;
        return espera<Sesion>({ token: `mock-${encontrado.id}`, usuario: encontrado });
      },
      async logout() { usuario = null; return espera(undefined); },
    },
    cursos: {
      listar: () => espera(db.cursos),
      estudiantes: () => espera(ESTUDIANTES),
      async actualizarPesos(cursoId, { cortes, actividades }) {
        exigirDocente();
        const c = db.cursos.find((x) => x.id === cursoId);
        if (c) c.pesos = cortes;
        for (const a of db.actividades) if (actividades[a.id] != null) a.peso = actividades[a.id];
        return espera(undefined);
      },
    },
    actividades: {
      listar: (cursoId) => espera(cursoId ? db.actividades.filter((a) => a.cursoId === cursoId) : db.actividades),
      async crear(input) {
        exigirDocente();
        const a = { ...input, id: `n${Date.now()}` };
        db.actividades.push(a);
        return espera(a);
      },
    },
    calificaciones: {
      porActividad: (id) => espera(db.califs.filter((c) => c.actividadId === id)),
      porCurso: (id) => {
        const ids = new Set(db.actividades.filter((a) => a.cursoId === id).map((a) => a.id));
        return espera(db.califs.filter((c) => ids.has(c.actividadId)));
      },
      delEstudiante: (id) => espera(db.califs.filter((c) => c.estudianteId === id)),
      async guardar({ actividadId, estudianteId, nota, retro, publicar }) {
        exigirDocente();
        let c = buscar(actividadId, estudianteId);
        if (!c) {
          c = { actividadId, estudianteId, entregado: null, archivo: null, nota: null, estado: null, retro: '' };
          db.califs.push(c);
        }
        Object.assign(c, { nota, retro, estado: publicar ? 'publicada' : 'borrador' } satisfies Partial<Calificacion>);
        return espera(c);
      },
      async publicarBorradores(actividadId) {
        exigirDocente();
        let n = 0;
        for (const c of db.califs) if (c.actividadId === actividadId && c.estado === 'borrador') { c.estado = 'publicada'; n++; }
        return espera(n);
      },
      async entregar(actividadId, archivo) {
        if (!usuario || usuario.rol !== 'estudiante') throw new ApiError(403, 'Inicia sesión como estudiante.');
        const act = db.actividades.find((a) => a.id === actividadId);
        if (!act || act.vence < cfg.hoy()) throw new ApiError(409, 'El plazo de entrega cerró.');
        let c = buscar(actividadId, usuario.id);
        if (c?.estado === 'publicada') throw new ApiError(409, 'La actividad ya fue calificada.');
        if (!c) {
          c = { actividadId, estudianteId: usuario.id, entregado: null, archivo: null, nota: null, estado: null, retro: '' };
          db.califs.push(c);
        }
        c.entregado = cfg.hoy();
        c.archivo = archivo.nombre;
        return espera(c);
      },
    },
  };
}
