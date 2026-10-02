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
  /** grupo, créditos y periodo aún no los expone el backend (ver CAMBIOS_CONTRATO.md). */
  grupo?: number;
  creditos?: number;
  periodo?: string;
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

// ── Respuestas del backend para el rol estudiante (contracts/evaluaciones.yaml y contracts/entregas.yaml) ──

export interface CorteMatriz {
  corte: Corte;
  pesoCorte: number;
  /** Nota publicada del corte; null mientras no se publique (nunca 0). */
  nota: number | null;
  publicado: boolean;
}

export interface CursoMatriz {
  cursoId: string;
  codigo: string;
  nombre: string;
  profesor: string;
  cortes: CorteMatriz[];
  /** Calculada por el backend solo sobre cortes publicados. */
  definitivaParcial: number;
  esParcial: boolean;
}

export interface MatrizNotas {
  cursos: CursoMatriz[];
}

export interface NotaActividad {
  actividadId: string;
  titulo: string;
  corte: Corte;
  peso: number;
  /** Instante UTC (ISO 8601). */
  fechaLimite: string;
  /** Un borrador también llega como SIN_CALIFICAR: el estudiante nunca lo ve. */
  estado: 'PUBLICADA' | 'SIN_CALIFICAR';
  nota: number | null;
  retroalimentacion: string | null;
}

export interface NotasActividades {
  cursoId: string;
  actividades: NotaActividad[];
}

export interface ArchivoDeEntrega {
  id: string;
  nombreArchivo: string;
  tamano: number;
}

export interface Entrega {
  id: string;
  actividadId: string;
  estudianteId: string;
  /** Instante UTC (ISO 8601). */
  fechaEnvio: string;
  estado: 'ENVIADA' | 'ANULADA';
  /** En el orden en que se subieron. */
  archivos: ArchivoDeEntrega[];
  tamanoTotal: number;
}

/** Archivo descargado, listo para guardar con su nombre original. */
export interface ArchivoDescargado {
  nombre: string;
  /** Blob en web y en React Native (fetch().blob()). */
  datos: Blob;
}
