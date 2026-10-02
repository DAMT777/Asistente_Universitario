export type Rol = 'estudiante' | 'docente';
export type Corte = 1 | 2 | 3;
export type FechaISO = string; // ISO 8601 UTC; vacío para una actividad sin fecha límite.
export type AcentoCurso = 'rojo' | 'violeta' | 'verde';

export interface Usuario {
  id: string;
  nombre: string;
  codigo: string;
  rol: Rol;
}

export interface Sesion {
  token: string;
  usuario: Usuario;
}

export interface Curso {
  id: string;
  codigo: string;
  nombre: string;
  grupo: number;
  creditos: number;
  periodo: string;
  docente: string;
  /** Peso de cada corte en la nota final; suman 100. */
  pesos: [number, number, number];
  monograma: string;
  acento: AcentoCurso;
}

export interface Actividad {
  id: string;
  cursoId: string;
  corte: Corte;
  titulo: string;
  /** Peso dentro de su corte; las actividades de un corte suman hasta 100. */
  peso: number;
  vence: FechaISO;
  requiereEntrega: boolean;
}

export type EstadoPublicacion = 'borrador' | 'publicada';

export interface Calificacion {
  actividadId: string;
  estudianteId: string;
  entregado: FechaISO | null;
  archivo: string | null;
  nota: number | null;
  estado: EstadoPublicacion | null;
  retro: string;
  /** Tamaño total de los archivos de la entrega. */
  tamano?: number;
  urlArchivo?: string;
  entregaId?: string;
  /** Archivos de la entrega vigente (uno o varios), en el orden en que se subieron. */
  archivos?: ArchivoEntregado[];
}

export interface ArchivoEntregado {
  id: string;
  nombre: string;
  tamano: number;
}

export interface PublicacionCorte {
  cursoId: string;
  estudianteId: string;
  corte: Corte;
  nota: number;
  fechaPublicacion: string;
}

/** Estado de una actividad visto por el docente. */
export type EstadoDocente = 'pendiente' | 'vencida' | 'sin_calificar' | 'borrador' | 'publicada';
/** Estado de una actividad visto por el estudiante (nunca ve borradores). */
export type EstadoEstudiante = 'pendiente' | 'vencida' | 'entregada' | 'calificada';
export type EstadoCalificacion = EstadoDocente | EstadoEstudiante;

export interface ResumenCorte {
  corte: Corte;
  peso: number;
  /** Suma acumulativa de los aportes publicados del corte, o null si no hay. */
  nota: number | null;
  /** Puntos que el corte aporta a la nota final (0–5). */
  aporte: number;
  /** Porcentaje del curso ya evaluado dentro de este corte. */
  evaluado: number;
}

export type Necesidad =
  | { tipo: 'aprobado' }
  | { tipo: 'cerrado' }
  | { tipo: 'inalcanzable' }
  | { tipo: 'necesita'; nota: number; restante: number };

export interface ResumenCurso {
  cortes: [ResumenCorte, ResumenCorte, ResumenCorte];
  acumulado: number;
  evaluado: number;
  necesidad: Necesidad;
}

export interface ArchivoEntrega {
  nombre: string;
  tamano: number;
  /** Blob en web, { uri } en React Native. El adaptador de api decide cómo subirlo. */
  datos?: unknown;
}
