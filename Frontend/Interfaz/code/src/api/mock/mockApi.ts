import type { ArchivoEntrega, Calificacion, Entrega, NotaActividad, Sesion, Usuario } from '@/types';

interface ArchivoGuardado { id: string; nombre: string; tamano: number; datos: Blob }
import { resumenCurso } from '@/domain';
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
  /** Entregas anuladas (actividadId:estudianteId); la calificación conserva el nombre del archivo. */
  const anuladas = new Set<string>();
  /** Archivos de cada entrega en memoria, para poder descargarlos. */
  const archivos = new Map<string, ArchivoGuardado[]>();
  const espera = <T>(v: T) => new Promise<T>((r) => setTimeout(() => r(structuredClone(v)), cfg.latenciaMs ?? 250));
  const buscar = (a: string, e: string) => db.califs.find((c) => c.actividadId === a && c.estudianteId === e);
  const exigirDocente = () => {
    if (usuario?.rol !== 'docente') throw new ApiError(403, 'Solo el docente puede hacer esto.', 'SIN_PERMISO');
  };
  const exigirEstudiante = (): Usuario => {
    if (usuario?.rol !== 'estudiante') throw new ApiError(403, 'Inicia sesión como estudiante.', 'SIN_PERMISO');
    return usuario;
  };
  /** Fin del día local de la fecha límite, como instante ISO. */
  const limite = (vence: string) => new Date(`${vence}T23:59:59`).toISOString();
  const clave = (actividadId: string, estudianteId: string) => `${actividadId}:${estudianteId}`;

  /** Las entregas de la semilla solo traen el nombre: se les da un contenido de ejemplo para poder descargarlas. */
  const archivosDe = (c: Calificacion): ArchivoGuardado[] => {
    const k = clave(c.actividadId, c.estudianteId);
    if (!archivos.has(k) && c.archivo) {
      const datos = new Blob([`Archivo de ejemplo de la api simulada: ${c.archivo}`], { type: 'text/plain' });
      archivos.set(k, [{ id: `arch-${k}-0`, nombre: c.archivo, tamano: datos.size, datos }]);
    }
    return archivos.get(k) ?? [];
  };

  const entregaDe = (c: Calificacion): Entrega | null => {
    const k = clave(c.actividadId, c.estudianteId);
    const estado = c.entregado ? 'ENVIADA' : anuladas.has(k) ? 'ANULADA' : null;
    const propios = archivosDe(c);
    if (!estado || propios.length === 0) return null;
    return {
      id: `ent-${k}`, actividadId: c.actividadId, estudianteId: c.estudianteId, estado,
      fechaEnvio: new Date(`${c.entregado ?? cfg.hoy()}T12:00:00`).toISOString(),
      archivos: propios.map((a) => ({ id: a.id, nombreArchivo: a.nombre, tamano: a.tamano })),
      tamanoTotal: propios.reduce((s, a) => s + a.tamano, 0),
    };
  };

  /** RN-05 / RN-06: hasta la fecha límite inclusive. */
  const exigirVigente = (actividadId: string) => {
    const act = db.actividades.find((a) => a.id === actividadId);
    if (!act) throw new ApiError(404, 'La actividad no existe.', 'NO_ENCONTRADO');
    if (act.vence < cfg.hoy()) throw new ApiError(422, 'La fecha límite de la actividad ya venció.', 'FECHA_LIMITE_VENCIDA');
  };

  const guardarArchivos = (actividadId: string, u: Usuario, nuevos: ArchivoEntrega[]): Entrega => {
    exigirVigente(actividadId);
    let c = buscar(actividadId, u.id);
    if (!c) {
      c = { actividadId, estudianteId: u.id, entregado: null, archivo: null, nota: null, estado: null, retro: '' };
      db.califs.push(c);
    }
    const k = clave(actividadId, u.id);
    c.entregado = cfg.hoy();
    c.archivo = nuevos.map((a) => a.nombre).join(', ');
    anuladas.delete(k);
    archivos.set(k, nuevos.map((a, i) => ({
      id: `arch-${k}-${Date.now()}-${i}`, nombre: a.nombre, tamano: a.tamano,
      datos: a.datos instanceof Blob ? a.datos : new Blob([], { type: 'application/octet-stream' }),
    })));
    return entregaDe(c)!;
  };

  const buscarEntrega = (entregaId: string, u: Usuario): Calificacion => {
    const c = db.califs.find((x) => x.estudianteId === u.id && `ent-${clave(x.actividadId, x.estudianteId)}` === entregaId);
    if (!c || !entregaDe(c)) throw new ApiError(404, 'La entrega no existe.', 'NO_ENCONTRADO');
    return c;
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
    },
    estudiante: {
      async matriz() {
        const u = exigirEstudiante();
        const propias = db.califs.filter((c) => c.estudianteId === u.id);
        return espera({
          cursos: db.cursos.map((curso) => {
            const r = resumenCurso(curso, db.actividades, propias);
            const cortes = r.cortes.map((k) => ({ corte: k.corte, pesoCorte: k.peso, nota: k.nota, publicado: k.nota != null }));
            return {
              cursoId: curso.id, codigo: curso.codigo, nombre: curso.nombre, profesor: curso.docente,
              cortes, definitivaParcial: r.acumulado, esParcial: cortes.some((k) => !k.publicado),
            };
          }),
        });
      },
      async notasCurso(cursoId) {
        const u = exigirEstudiante();
        if (!db.cursos.some((c) => c.id === cursoId)) throw new ApiError(404, 'El curso no existe.', 'NO_ENCONTRADO');
        const actividades = db.actividades.filter((a) => a.cursoId === cursoId).map((a): NotaActividad => {
          const c = buscar(a.id, u.id);
          const publicada = c?.estado === 'publicada';
          return {
            actividadId: a.id, titulo: a.titulo, corte: a.corte, peso: a.peso, fechaLimite: limite(a.vence),
            estado: publicada ? 'PUBLICADA' : 'SIN_CALIFICAR', nota: publicada ? c!.nota : null, retroalimentacion: publicada ? c!.retro : null,
          };
        });
        return espera({ cursoId, actividades });
      },
      async misEntregas() {
        const u = exigirEstudiante();
        return espera(db.califs.filter((c) => c.estudianteId === u.id).map(entregaDe).filter((e): e is Entrega => e != null));
      },
      async subirEntrega(actividadId, nuevos) {
        return espera(guardarArchivos(actividadId, exigirEstudiante(), nuevos));
      },
      async editarEntrega(entregaId, nuevos) {
        const u = exigirEstudiante();
        return espera(guardarArchivos(buscarEntrega(entregaId, u).actividadId, u, nuevos));
      },
      async anularEntrega(entregaId) {
        const u = exigirEstudiante();
        const c = buscarEntrega(entregaId, u);
        exigirVigente(c.actividadId);
        c.entregado = null;
        anuladas.add(clave(c.actividadId, u.id));
        return espera(entregaDe(c)!);
      },
      async descargarArchivo(entregaId, archivoId) {
        const a = archivosDe(buscarEntrega(entregaId, exigirEstudiante())).find((x) => x.id === archivoId);
        if (!a) throw new ApiError(404, 'El archivo no existe.', 'NO_ENCONTRADO');
        // Sin structuredClone: el Blob se entrega tal cual.
        return new Promise((r) => setTimeout(() => r({ nombre: a.nombre, datos: a.datos }), cfg.latenciaMs ?? 250));
      },
    },
  };
}
