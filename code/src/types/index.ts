export type Rol = 'estudiante' | 'docente';
export type Corte = 1 | 2 | 3;
export type FechaISO = string; // 'YYYY-MM-DD'
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
  /** Peso dentro de su corte; las actividades de un corte suman 100. */
  peso: number;
  vence: FechaISO;
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
}

/** Estado de una actividad visto por el docente. */
export type EstadoDocente = 'pendiente' | 'vencida' | 'sin_calificar' | 'borrador' | 'publicada';
/** Estado de una actividad visto por el estudiante (nunca ve borradores). */
export type EstadoEstudiante = 'pendiente' | 'vencida' | 'entregada' | 'calificada';
export type EstadoCalificacion = EstadoDocente | EstadoEstudiante;

export interface ResumenCorte {
  corte: Corte;
  peso: number;
  /** Promedio ponderado de las notas publicadas del corte, o null si no hay. */
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
