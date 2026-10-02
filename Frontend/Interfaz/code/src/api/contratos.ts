import type { Actividad, ArchivoEntrega, Calificacion, Corte, Curso, PublicacionCorte, Sesion, Usuario } from '@/types';

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
    /** El rol no se elige: lo decide el servicio de usuarios según la cuenta. */
    login(input: { usuario: string; contrasena: string }): Promise<Sesion>;
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
    /** Uno o varios archivos. Si ya hay una entrega vigente, el conjunto la reemplaza. */
    entregar(actividadId: string, archivos: ArchivoEntrega[]): Promise<Calificacion>;
    anularEntrega(actividadId: string): Promise<void>;
    /** URL local (object URL) de un archivo de la entrega, para descargarlo con su nombre. */
    descargarArchivo(entregaId: string, archivoId: string): Promise<string>;
  };
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}
