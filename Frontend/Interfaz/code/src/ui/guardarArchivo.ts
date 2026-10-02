/** Web: guarda un Blob con su nombre original, sea del tipo que sea. En React Native se usa el sistema de archivos. */
export function guardarArchivo(datos: Blob, nombre: string): void {
  const url = URL.createObjectURL(datos);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = nombre;
  enlace.rel = 'noopener';
  document.body.appendChild(enlace);
  enlace.click();
  enlace.remove();
  // Se libera después de que el navegador tome el archivo.
  setTimeout(() => URL.revokeObjectURL(url), 30_000);
}
