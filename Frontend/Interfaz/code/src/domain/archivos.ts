/** "820 KB", "1.5 MB": tamaños legibles con un decimal como máximo. */
export function formatoTamano(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  const kb = bytes / 1024;
  if (kb < 1024) return `${Math.round(kb)} KB`;
  return `${(kb / 1024).toFixed(1).replace(/\.0$/, '')} MB`;
}

/** Extensión en mayúsculas para el ícono del archivo ("PDF", "DOCX"…). */
export function extension(nombre: string): string {
  const i = nombre.lastIndexOf('.');
  return i < 0 ? '' : nombre.slice(i + 1).toUpperCase();
}
