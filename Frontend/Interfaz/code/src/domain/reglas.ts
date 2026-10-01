export const NOTA_MIN = 0;
export const NOTA_MAX = 5;
export const NOTA_APROBATORIA = 3.0;

export interface ValidacionPesos {
  total: number;
  ok: boolean;
}

/** Cada peso es entero > 0 y el grupo suma exactamente 100. */
export function validarPesos(valores: Array<number | string>): ValidacionPesos {
  const enteros = valores.every((p) => /^\d+$/.test(String(p)) && Number(p) > 0);
  const total = valores.reduce<number>((a, b) => a + (Number(b) || 0), 0);
  return { total, ok: enteros && total === 100 };
}

/** Un estudiante solo puede subir mientras no esté calificado y el plazo siga abierto. */
export function puedeEntregar(vence: string, hoy: string, calificada: boolean): boolean {
  return !calificada && vence >= hoy;
}
