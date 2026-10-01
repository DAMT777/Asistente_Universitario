const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

export function hoyISO(d = new Date()): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

export function diasEntre(desde: string, hasta: string): number {
  return Math.round((Date.parse(hasta) - Date.parse(desde)) / 864e5);
}

export function fechaCorta(iso: string): string {
  const [, m, d] = iso.split('-');
  return `${Number(d)} ${MESES[Number(m) - 1]}`;
}

export function diaMes(iso: string): { dia: string; mes: string } {
  const [, m, d] = iso.split('-');
  return { dia: String(Number(d)), mes: MESES[Number(m) - 1].toUpperCase() };
}

export function relativo(iso: string, hoy: string): string {
  const d = diasEntre(hoy, iso);
  if (d === 0) return 'hoy';
  if (d === 1) return 'mañana';
  return d > 0 ? `en ${d} días` : `hace ${-d} días`;
}

export function iniciales(nombre: string): string {
  const w = nombre.trim().split(/\s+/);
  const apellido = w.length >= 4 ? w[2] : w[1] ?? '';
  return (w[0][0] + (apellido[0] ?? '')).toUpperCase();
}
