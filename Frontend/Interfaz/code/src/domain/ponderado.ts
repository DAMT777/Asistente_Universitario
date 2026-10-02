import type { Actividad, Calificacion, Corte, Curso, Necesidad, ResumenCorte, ResumenCurso } from '@/types';
import { NOTA_APROBATORIA, NOTA_MAX } from './reglas';

const CORTES: Corte[] = [1, 2, 3];

/**
 * Motor de ponderado.
 * - Nota del corte: suma de aportes de las notas PUBLICADAS, sin normalizar.
 * - Aporte del corte: Σ(nota × pesoActividad) / 100 × pesoCorte / 100.
 * - Acumulado: suma de aportes (escala 0–5, como la "definitiva" parcial del SIAU).
 */
export function resumenCurso(
  curso: Curso,
  actividades: Actividad[],
  califsEstudiante: Calificacion[],
  aprobatoria = NOTA_APROBATORIA,
): ResumenCurso {
  const porAct = new Map(califsEstudiante.map((c) => [c.actividadId, c]));
  const cortes = CORTES.map((k): ResumenCorte => {
    let sumaPesos = 0;
    let sumaNotas = 0;
    for (const a of actividades) {
      if (a.cursoId !== curso.id || a.corte !== k) continue;
      const c = porAct.get(a.id);
      if (c?.estado === 'publicada' && c.nota != null) {
        sumaPesos += a.peso;
        sumaNotas += c.nota * a.peso;
      }
    }
    const pc = curso.pesos[k - 1];
    return {
      corte: k,
      peso: pc,
      nota: sumaPesos ? sumaNotas / 100 : null,
      aporte: ((sumaNotas / 100) * pc) / 100,
      evaluado: (sumaPesos * pc) / 100,
    };
  }) as ResumenCurso['cortes'];

  const acumulado = cortes.reduce((a, c) => a + c.aporte, 0);
  const evaluado = cortes.reduce((a, c) => a + c.evaluado, 0);
  return { cortes, acumulado, evaluado, necesidad: calcularNecesidad(acumulado, evaluado, aprobatoria) };
}

/** Promedio que necesita el estudiante en lo que falta por evaluar para llegar a la aprobatoria. */
export function calcularNecesidad(acumulado: number, evaluado: number, aprobatoria = NOTA_APROBATORIA): Necesidad {
  if (acumulado >= aprobatoria - 1e-9) return { tipo: 'aprobado' };
  const restante = Math.round(100 - evaluado);
  if (restante <= 0) return { tipo: 'cerrado' };
  const nota = (aprobatoria - acumulado) / (restante / 100);
  if (nota > NOTA_MAX) return { tipo: 'inalcanzable' };
  return { tipo: 'necesita', nota, restante };
}

export function redondear1(n: number): number {
  return Math.round(n * 10) / 10;
}

export function formatoNota(n: number | null | undefined): string {
  return n == null ? '—' : redondear1(n).toFixed(1);
}

export function describirNecesidad(n: Necesidad, aprobatoria = NOTA_APROBATORIA): string {
  const ap = aprobatoria.toFixed(1);
  switch (n.tipo) {
    case 'aprobado': return `Ya superas ${ap} con lo acumulado.`;
    case 'cerrado': return 'Curso cerrado.';
    case 'inalcanzable': return `No alcanza ${ap} aunque saques 5.0 en lo que falta.`;
    case 'necesita': return `Necesitas ${formatoNota(n.nota)} en promedio en el ${n.restante}% restante.`;
  }
}
