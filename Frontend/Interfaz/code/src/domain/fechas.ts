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
  if (!iso) return { dia: '—', mes: '' };
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

/** Hace cuánto pasó algo, para avisos: «Ahora», «Hace 5 min», «Hace 3 h», «Ayer», «Hace 4 días» o la fecha corta. */
export function haceCuanto(iso: string, ahora: number = Date.now()): string {
  const minutos = Math.floor((ahora - Date.parse(iso)) / 60_000);
  if (!Number.isFinite(minutos)) return '';
  if (minutos < 1) return 'Ahora';
  if (minutos < 60) return `Hace ${minutos} min`;
  const horas = Math.floor(minutos / 60);
  if (horas < 24) return `Hace ${horas} h`;
  const dias = Math.floor(horas / 24);
  if (dias === 1) return 'Ayer';
  if (dias < 7) return `Hace ${dias} días`;
  return fechaCorta(iso);
}

export function iniciales(nombre: string): string {
  const w = nombre.trim().split(/\s+/);
  const apellido = w.length >= 4 ? w[2] : w[1] ?? '';
  return (w[0][0] + (apellido[0] ?? '')).toUpperCase();
}
