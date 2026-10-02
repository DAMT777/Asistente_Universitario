const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

export function hoyISO(d = new Date()): string { return d.toISOString(); }

export function diasEntre(desde: string, hasta: string): number {
  return Math.round((Date.parse(hasta) - Date.parse(desde)) / 864e5);
}

export function fechaCorta(iso: string): string {
  if (!iso) return 'Sin fecha límite';
  return new Intl.DateTimeFormat('es-CO', { timeZone: 'America/Bogota', day: 'numeric', month: 'short' }).format(new Date(iso));
}

export function fechaCompleta(iso: string): string {
  if (!iso) return 'Sin fecha límite';
  return new Intl.DateTimeFormat('es-CO', { timeZone: 'America/Bogota', dateStyle: 'medium', timeStyle: 'short' }).format(new Date(iso)) + ' · hora de Colombia';
}

export function fechaFormulario(valor: string): string {
  return valor ? new Date(valor + ':00-05:00').toISOString() : '';
}

export function diaMes(iso: string): { dia: string; mes: string } {
  if (!iso) return { dia: '—', mes: 'SIN FECHA' };
  const partes = new Intl.DateTimeFormat('es-CO', { timeZone: 'America/Bogota', day: 'numeric', month: 'numeric' }).formatToParts(new Date(iso));
  return { dia: partes.find(p => p.type === 'day')!.value, mes: MESES[Number(partes.find(p => p.type === 'month')!.value) - 1].toUpperCase() };
}

export function relativo(iso: string, hoy: string): string {
  const fechaLocal = (f: string) => new Intl.DateTimeFormat('en-CA', { timeZone: 'America/Bogota', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date(f));
  const d = diasEntre(fechaLocal(hoy), fechaLocal(iso));
  if (d === 0) return 'hoy';
  if (d === 1) return 'mañana';
  return d > 0 ? `en ${d} días` : `hace ${-d} días`;
}

export function iniciales(nombre: string): string {
  const w = nombre.trim().split(/\s+/);
  const apellido = w.length >= 4 ? w[2] : w[1] ?? '';
  return (w[0][0] + (apellido[0] ?? '')).toUpperCase();
}
