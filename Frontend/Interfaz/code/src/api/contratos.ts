import type { Actividad, ArchivoEntrega, Calificacion, Curso, Rol, Sesion, Usuario } from '@/types';

/**
 * Contrato único que consumen los hooks. Hay dos implementaciones:
 * - createHttpApi: REST real (fetch, funciona igual en web y React Native).
 * - createMockApi: memoria con datos de ejemplo para desarrollo y demos.
 */
export interface ApiClient {
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
  };
  calificaciones: {
    porActividad(actividadId: string): Promise<Calificacion[]>;
    porCurso(cursoId: string): Promise<Calificacion[]>;
    delEstudiante(estudianteId: string): Promise<Calificacion[]>;
    guardar(input: { actividadId: string; estudianteId: string; nota: number; retro: string; publicar: boolean }): Promise<Calificacion>;
    publicarBorradores(actividadId: string): Promise<number>;
    entregar(actividadId: string, archivo: ArchivoEntrega): Promise<Calificacion>;
  };
}

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}
