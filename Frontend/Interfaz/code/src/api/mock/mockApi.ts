import type { Calificacion, PublicacionCorte, Sesion, Usuario } from '@/types';
import { resumenCurso, redondear1, validarPesos, puedeEntregar } from '@/domain';
import { seleccionEntregaSchema, actividadInputSchema } from '@/schemas';
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
    publicaciones: [] as PublicacionCorte[],
  };
  let usuario: Usuario | null = null;
  /** Contenido de los archivos entregados en la demostración, por id de archivo. */
  const contenidos = new Map<string, Blob>();
  const espera = <T>(v: T) => new Promise<T>((r) => setTimeout(() => r(structuredClone(v)), cfg.latenciaMs ?? 250));
  const buscar = (a: string, e: string) => db.califs.find((c) => c.actividadId === a && c.estudianteId === e);
  const exigirDocente = () => {
    if (usuario?.rol !== 'docente') throw new ApiError(403, 'Solo el docente puede hacer esto.');
  };

  // Datos semilla: primer corte publicado; los demás siguen pendientes.
  for (const curso of db.cursos) for (const estudiante of ESTUDIANTES) {
    const resumen = resumenCurso(curso, db.actividades, db.califs.filter(c => c.estudianteId === estudiante.id));
    const nota = resumen.cortes[0].nota;
    if (nota !== null) db.publicaciones.push({ cursoId: curso.id, estudianteId: estudiante.id, corte: 1, nota: redondear1(nota), fechaPublicacion: '2026-09-15T15:00:00Z' });
  }
  return {
    cortes: {
      async listar(cursoId) {
        if (!usuario) throw new ApiError(401, 'Inicia sesión.');
        return espera(db.publicaciones.filter(p => (!cursoId || p.cursoId === cursoId) && (usuario!.rol === 'docente' || p.estudianteId === usuario!.id)));
      },
      async publicar({ cursoId, estudianteId, corte, omitirBorradores, corregir }) {
        exigirDocente();
        const curso = db.cursos.find(c => c.id === cursoId);
        if (!curso || !ESTUDIANTES.some(e => e.id === estudianteId)) throw new ApiError(404, 'Curso o estudiante no encontrado.');
        const acts = db.actividades.filter(a => a.cursoId === cursoId && a.corte === corte);
        const califs = db.califs.filter(c => c.estudianteId === estudianteId && acts.some(a => a.id === c.actividadId));
        if (!omitirBorradores && califs.some(c => c.estado === 'borrador')) throw new ApiError(422, 'Hay notas en borrador para este estudiante. Publícalas o indica expresamente que deseas omitirlas.');
        const nota = resumenCurso(curso, acts, califs).cortes[corte - 1].nota;
        if (nota === null) throw new ApiError(422, 'Este estudiante no tiene calificaciones publicadas en el corte.');
        const existente = db.publicaciones.find(p => p.cursoId === cursoId && p.estudianteId === estudianteId && p.corte === corte);
        if (!!existente !== corregir) throw new ApiError(409, 'La publicación cambió. Actualiza la información e intenta de nuevo.');
        const nueva = { cursoId, estudianteId, corte, nota: redondear1(nota), fechaPublicacion: cfg.hoy() };
        if (existente) Object.assign(existente, nueva); else db.publicaciones.push(nueva);
        return espera(nueva);
      },
    },
    auth: {
      async login({ usuario: u }) {
        // Como el servicio real: el rol sale de la cuenta, no de lo que elija la persona.
        const encontrado = [DOCENTE, ...ESTUDIANTES].find((x) => x.codigo === u) ?? null;
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
        if (!c) throw new ApiError(404, 'Curso no encontrado.');
        if (!validarPesos(cortes).ok) throw new ApiError(422, 'Los cortes deben sumar 100%.');
        const propias = db.actividades.filter(a => a.cursoId === cursoId);
        if (Object.keys(actividades).some(id => !propias.some(a => a.id === id))) throw new ApiError(422, 'La actividad no pertenece al curso.');
        for (const corte of [1, 2, 3]) if (!validarPesos(propias.filter(a => a.corte === corte).map(a => actividades[a.id] ?? a.peso), false).ok) throw new ApiError(422, 'Los pesos de actividades no pueden superar 100%.');
        c.pesos = cortes;
        for (const a of db.actividades) if (a.cursoId === cursoId && actividades[a.id] != null) a.peso = actividades[a.id];
        return espera(undefined);
      },
    },
    actividades: {
      listar: (cursoId) => espera(cursoId ? db.actividades.filter((a) => a.cursoId === cursoId) : db.actividades),
      async editar(id, input) {
        exigirDocente();
        const a = db.actividades.find(x => x.id === id && x.cursoId === input.cursoId);
        if (!a) throw new ApiError(404, 'Actividad no encontrada.');
        // Permite conservar una fecha ya vencida al editar título o peso.
        const valor = actividadInputSchema(input.vence || cfg.hoy()).parse(input);
        const total = db.actividades.filter(x => x.id !== id && x.cursoId === input.cursoId && x.corte === input.corte).reduce((n,x) => n + x.peso, 0);
        if (total + valor.peso > 100) throw new ApiError(422, 'El corte no puede superar 100%.');
        Object.assign(a, valor);
        return espera(a);
      },
      async crear(input) {
        exigirDocente();
        if (db.actividades.filter(a => a.cursoId === input.cursoId && a.corte === input.corte).reduce((n, a) => n + a.peso, 0) + input.peso > 100) throw new ApiError(422, 'El corte no puede superar 100%.');
        actividadInputSchema(cfg.hoy()).parse(input);
        const a = { ...input, id: `n${Date.now()}` };
        db.actividades.push(a);
        return espera(a);
      },
    },
    calificaciones: {
      async descargarArchivo(id, archivoId) {
        const c = db.califs.find(c => c.entregaId === id);
        if (!usuario || !c || (usuario.rol !== 'docente' && usuario.id !== c.estudianteId)) throw new ApiError(403, 'No tienes acceso a este archivo.');
        const datos = c.archivos?.some(a => a.id === archivoId) ? contenidos.get(archivoId) : undefined;
        if (!datos) throw new ApiError(404, 'El archivo no está disponible en la demostración.');
        // Una URL nueva por descarga: quien la usa la libera después.
        return URL.createObjectURL(datos);
      },
      porActividad: async (id) => { exigirDocente(); return espera(db.califs.filter((c) => c.actividadId === id)); },
      porCurso: async (id) => {
        exigirDocente();
        const ids = new Set(db.actividades.filter((a) => a.cursoId === id).map((a) => a.id));
        return espera(db.califs.filter((c) => ids.has(c.actividadId)));
      },
      delEstudiante: async (id) => {
        if (!usuario || usuario.id !== id || usuario.rol !== 'estudiante') throw new ApiError(403, 'Solo puedes consultar tus propias notas.');
        return espera(db.califs.filter(c => c.estudianteId === id).map(c => c.estado === 'publicada' ? c : { ...c, nota: null, estado: null, retro: '' }));
      },
      async guardar({ actividadId, estudianteId, nota, retro, publicar }) {
        // entregaId no se usa aquí: en el mock la entrega vive en la misma fila de la calificación.
        exigirDocente();
        if (!db.actividades.some(a => a.id === actividadId) || !ESTUDIANTES.some(e => e.id === estudianteId)) throw new ApiError(404, 'Actividad o estudiante no encontrado.');
        if (!Number.isFinite(nota) || nota < 0 || nota > 5) throw new ApiError(422, 'La nota debe estar entre 0.0 y 5.0.');
        let c = buscar(actividadId, estudianteId);
        if (!c) {
          c = { actividadId, estudianteId, entregado: null, archivo: null, nota: null, estado: null, retro: '' };
          db.califs.push(c);
        }
        // Igual que el servicio: si no cambió nada conserva su estado; si cambió, vuelve a borrador (CU-07).
        const texto = retro.trim();
        if (!publicar && c.estado && c.nota === nota && c.retro === texto) return espera(c);
        Object.assign(c, { nota, retro: texto, estado: publicar ? 'publicada' : 'borrador' } satisfies Partial<Calificacion>);
        return espera(c);
      },
      async publicarBorradores(actividadId) {
        exigirDocente();
        let n = 0;
        for (const c of db.califs) if (c.actividadId === actividadId && c.estado === 'borrador') { c.estado = 'publicada'; n++; }
        return espera(n);
      },
      async anularEntrega(actividadId) {
        if (!usuario || usuario.rol !== 'estudiante') throw new ApiError(403, 'Inicia sesión como estudiante.');
        const act = db.actividades.find(a => a.id === actividadId);
        if (!act || !puedeEntregar(act.vence, cfg.hoy(), act.requiereEntrega)) throw new ApiError(422, 'El plazo de entrega cerró.');
        const c = buscar(actividadId, usuario.id);
        if (!c?.entregado) throw new ApiError(404, 'No hay una entrega activa.');
        for (const a of c.archivos ?? []) contenidos.delete(a.id);
        Object.assign(c, { entregado: null, archivo: null, tamano: undefined, urlArchivo: undefined, archivos: undefined });
        return espera(undefined);
      },
      async entregar(actividadId, archivos) {
        if (!usuario || usuario.rol !== 'estudiante') throw new ApiError(403, 'Inicia sesión como estudiante.');
        const act = db.actividades.find((a) => a.id === actividadId);
        if (!act) throw new ApiError(404, 'Actividad no encontrada.');
        if (!act.requiereEntrega) throw new ApiError(422, 'Esta actividad no requiere entrega de archivo.');
        if (!puedeEntregar(act.vence, cfg.hoy(), act.requiereEntrega)) throw new ApiError(422, 'El plazo de entrega cerró.');
        let c = buscar(actividadId, usuario.id);
        seleccionEntregaSchema.parse(archivos);
        if (!c) {
          c = { actividadId, estudianteId: usuario.id, entregado: null, archivo: null, nota: null, estado: null, retro: '' };
          db.califs.push(c);
        }
        for (const a of c.archivos ?? []) contenidos.delete(a.id);
        c.entregaId = `entrega-${actividadId}-${usuario.id}`;
        c.archivos = archivos.map((a, i) => ({ id: `${c!.entregaId}-${Date.now()}-${i}`, nombre: a.nombre, tamano: a.tamano }));
        c.archivos.forEach((a, i) => { const datos = archivos[i].datos; if (datos instanceof Blob) contenidos.set(a.id, datos); });
        c.tamano = archivos.reduce((s, a) => s + a.tamano, 0);
        c.entregado = cfg.hoy();
        c.archivo = archivos.map(a => a.nombre).join(', ');
        return espera(c);
      },
    },
  };
}
