import type { Actividad, ArchivoEntrega, Calificacion, Corte, Curso, PublicacionCorte, Rol, Sesion, Usuario } from '@/types';

/**
 * Contrato único que consumen los hooks. Hay dos implementaciones:
 * - createHttpApi: REST real (fetch, funciona igual en web y React Native).
 * - createMockApi: memoria con datos de ejemplo para desarrollo y demos.
 */
export interface ApiClient {
  cortes: {
    listar(cursoId?: string): Promise<PublicacionCorte[]>;
    publicar(input: { cursoId: string; estudianteId: string; corte: Corte; omitirBorradores: boolean; corregir: boolean }): Promise<PublicacionCorte>;
  };
  auth: {
    login(input: { usuario: string; contrasena: string; rol: Rol }): Promise<Sesion>;
    logout(): Promise<void>;
  };
  cursos: {
    listar(): Promise<Curso[]>;
    estudiantes(cursoId: string): Promise<Usuario[]>;
    actualizarPesos(cursoId: string, input: { cortes: [number, number, number]; actividades: Record<string, number> }): Promise<void>;
  };
  actividades: {
    listar(cursoId?: string): Promise<Actividad[]>;
    crear(input: Omit<Actividad, 'id'>): Promise<Actividad>;
    editar(id: string, input: Omit<Actividad, 'id'>): Promise<Actividad>;
  };
  calificaciones: {
    porActividad(actividadId: string): Promise<Calificacion[]>;
    porCurso(cursoId: string): Promise<Calificacion[]>;
    delEstudiante(estudianteId: string): Promise<Calificacion[]>;
    /** Crea o modifica la nota (CU-05, CU-06, CU-07). entregaId identifica la entrega calificada en CU-05. */
    guardar(input: { actividadId: string; estudianteId: string; nota: number; retro: string; publicar: boolean; entregaId?: string }): Promise<Calificacion>;
    /** CU-08. Publica los borradores de la actividad y devuelve cuántos se publicaron. */
    publicarBorradores(actividadId: string): Promise<number>;
    entregar(actividadId: string, archivo: ArchivoEntrega): Promise<Calificacion>;
    anularEntrega(actividadId: string): Promise<void>;
    descargarArchivo(entregaId: string): Promise<string>;
  };
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}
