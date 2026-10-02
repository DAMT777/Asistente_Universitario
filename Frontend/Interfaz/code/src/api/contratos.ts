import type { Actividad, ArchivoDescargado, ArchivoEntrega, Calificacion, Curso, Entrega, MatrizNotas, NotasActividades, Rol, Sesion, Usuario } from '@/types';

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
    guardar(input: { actividadId: string; estudianteId: string; nota: number; retro: string; publicar: boolean }): Promise<Calificacion>;
    publicarBorradores(actividadId: string): Promise<number>;
  };
  /**
   * Rol estudiante, con las mismas rutas y formas de contracts/ (CU-13 a CU-16).
   * El estudiante es el del token: ninguna ruta recibe su id.
   */
  estudiante: {
    /** GET /mis-notas: cortes publicados y definitiva parcial por curso. */
    matriz(): Promise<MatrizNotas>;
    /** GET /mis-notas/cursos/{cursoId}/actividades: nota publicada o SIN_CALIFICAR por actividad. */
    notasCurso(cursoId: string): Promise<NotasActividades>;
    /** GET /mis-entregas: entregas propias, enviadas y anuladas. */
    misEntregas(): Promise<Entrega[]>;
    /** POST /actividades/{id}/entregas con uno o varios archivos. Si ya existía una entrega (aun anulada), se reutiliza. */
    subirEntrega(actividadId: string, archivos: ArchivoEntrega[]): Promise<Entrega>;
    /** PUT /entregas/{id}: reemplaza todos los archivos mientras no venza la fecha. */
    editarEntrega(entregaId: string, archivos: ArchivoEntrega[]): Promise<Entrega>;
    /** DELETE /entregas/{id}: anula mientras no venza la fecha. */
    anularEntrega(entregaId: string): Promise<Entrega>;
    /** GET /entregas/{id}/archivos/{archivoId}: contenido con su nombre original. */
    descargarArchivo(entregaId: string, archivoId: string): Promise<ArchivoDescargado>;
  };
}

export class ApiError extends Error {
  /** codigo: código estable del backend (FECHA_LIMITE_VENCIDA, SIN_PERMISO…), si vino en la respuesta. */
  constructor(public status: number, message: string, public codigo?: string) {
    super(message);
  }
}
