export const NOTA_MIN = 0;
export const NOTA_MAX = 5;
export const NOTA_APROBATORIA = 3.0;

export interface ValidacionPesos {
  total: number;
  ok: boolean;
}

/** Cada peso es entero > 0; los cortes suman 100 y las actividades admiten hasta 100. */
export function validarPesos(valores: Array<number | string>, exacto = true): ValidacionPesos {
  const enteros = valores.every((p) => /^\d+$/.test(String(p)) && Number(p) > 0 && Number(p) <= 100);
  const total = valores.reduce<number>((a, b) => a + (Number(b) || 0), 0);
  return { total, ok: enteros && (exacto ? total === 100 : total <= 100) };
}

/** Un estudiante puede subir, reemplazar o anular mientras el plazo siga abierto, incluso si existe nota. */
export function puedeEntregar(vence: string, hoy: string, requiereEntrega: boolean): boolean {
  return requiereEntrega && !!vence && Date.parse(hoy) <= Date.parse(vence);
}
