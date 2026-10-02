import type {
  AcentoCurso, Actividad, Calificacion, Corte, Curso, CursoMatriz, Entrega, NotaActividad, ResumenCorte, ResumenCurso,
} from '@/types';
import { hoyISO } from './fechas';
import { calcularNecesidad } from './ponderado';
import { NOTA_APROBATORIA } from './reglas';

/**
 * Traduce las respuestas del backend (rol estudiante) a los modelos que usan las pantallas.
 * La nota de cada corte y la definitiva parcial vienen calculadas por el backend: aquí no se recalculan.
 */

const ACENTOS: AcentoCurso[] = ['rojo', 'violeta', 'verde'];
const CORTES: Corte[] = [1, 2, 3];

/** Iniciales de las dos primeras palabras significativas: "Simulación Computacional" → "SC". */
export function monograma(nombre: string): string {
  const sinTildes = nombre.normalize('NFD').replace(/\p{Diacritic}/gu, '');
  const palabras = sinTildes.split(/\s+/).filter((p) => p.length > 2);
  return (palabras.slice(0, 2).map((p) => p[0]).join('') || sinTildes.slice(0, 2)).toUpperCase();
}

/** Fecha local (YYYY-MM-DD) de un instante ISO; las pantallas comparan y muestran días. */
export function fechaLocal(instante: string): string {
  return hoyISO(new Date(instante));
}

export function cursoDesdeMatriz(c: CursoMatriz, indice: number): Curso {
  const peso = (k: Corte) => c.cortes.find((x) => x.corte === k)?.pesoCorte ?? 0;
  return {
    id: c.cursoId,
    codigo: c.codigo,
    nombre: c.nombre,
    docente: c.profesor,
    pesos: [peso(1), peso(2), peso(3)],
    monograma: monograma(c.nombre),
    acento: ACENTOS[indice % ACENTOS.length],
  };
}

/** Resumen del curso con la nota publicada de cada corte (RN-14) y la definitiva parcial del backend. */
export function resumenDesdeMatriz(c: CursoMatriz, aprobatoria = NOTA_APROBATORIA): ResumenCurso {
  const cortes = CORTES.map((k): ResumenCorte => {
    const x = c.cortes.find((y) => y.corte === k);
    const peso = x?.pesoCorte ?? 0;
    const nota = x?.publicado ? x.nota : null;
    return { corte: k, peso, nota, aporte: nota == null ? 0 : (nota * peso) / 100, evaluado: nota == null ? 0 : peso };
  }) as ResumenCurso['cortes'];
  const evaluado = cortes.reduce((a, k) => a + k.evaluado, 0);
  return { cortes, acumulado: c.definitivaParcial, evaluado, necesidad: calcularNecesidad(c.definitivaParcial, evaluado, aprobatoria) };
}

export function actividadDesdeNota(cursoId: string, n: NotaActividad): Actividad {
  return { id: n.actividadId, cursoId, corte: n.corte, titulo: n.titulo, peso: n.peso, vence: fechaLocal(n.fechaLimite) };
}

/**
 * Une la nota publicada y la entrega vigente de una actividad en el modelo Calificacion.
 * Sin nota publicada ni entrega enviada no hay calificación (sin calificar no es 0).
 */
export function calificacionDesde(n: NotaActividad, entrega: Entrega | undefined, estudianteId: string): Calificacion | undefined {
  const publicada = n.estado === 'PUBLICADA' && n.nota != null;
  const enviada = entrega?.estado === 'ENVIADA' ? entrega : undefined;
  if (!publicada && !enviada) return undefined;
  return {
    actividadId: n.actividadId,
    estudianteId,
    entregado: enviada ? fechaLocal(enviada.fechaEnvio) : null,
    archivo: enviada?.nombreArchivo ?? null,
    nota: publicada ? n.nota : null,
    estado: publicada ? 'publicada' : null,
    retro: publicada ? n.retroalimentacion ?? '' : '',
  };
}
