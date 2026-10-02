import { useCallback, useEffect, useState } from 'react';
import { acentosOscuros, estadosOscuros, paleta, paletaOscura, variableColor, vidrioClaro, vidrioOscuro } from '@/theme/tokens';

/**
 * Declara en <head> las variables CSS de cada tema a partir de los tokens, para que la paleta
 * siga teniendo una sola fuente. Los temas se activan con atributos en <html>:
 *   data-au-tema="oscuro"  → modo nocturno (desde Accesibilidad)
 *   data-au-vidrio="si"    → modo vidrio (botón del encabezado)
 */
export function instalarTema() {
  if (document.getElementById('au-tema')) return;
  const colores = (p: Partial<Record<string, string>>) =>
    Object.entries(p).map(([k, v]) => `${variableColor(k)}:${v};`).join('');
  const estados = Object.entries(estadosOscuros)
    .map(([k, e]) => `--au-estado-${k}-fondo:${e.fondo};--au-estado-${k}-texto:${e.texto};`).join('');
  const acentos = Object.entries(acentosOscuros).map(([k, v]) => `--au-acento-${k}:${v};`).join('');

  const estilo = document.createElement('style');
  estilo.id = 'au-tema';
  estilo.textContent = [
    `:root{${colores(paleta)}}`,
    `html[data-au-tema="oscuro"]{${colores(paletaOscura)}${estados}${acentos}}`,
    `html[data-au-vidrio="si"]{${colores(vidrioClaro)}}`,
    `html[data-au-vidrio="si"][data-au-tema="oscuro"]{${colores(vidrioOscuro)}}`,
  ].join('\n');
  document.head.prepend(estilo);
}

const CLAVE_VIDRIO = 'aula.vidrio';

function leerVidrio(): boolean {
  try { return localStorage.getItem(CLAVE_VIDRIO) === 'si'; } catch { return false; }
}

function aplicarVidrio(activo: boolean) {
  if (activo) document.documentElement.setAttribute('data-au-vidrio', 'si');
  else document.documentElement.removeAttribute('data-au-vidrio');
}

/** Aplica el modo vidrio guardado antes del primer render, para que no parpadee. */
export function aplicarVidrioGuardado() {
  aplicarVidrio(leerVidrio());
}

/** Estado del modo vidrio, guardado en este navegador. */
export function useVidrio(): [boolean, () => void] {
  const [activo, setActivo] = useState(leerVidrio);
  useEffect(() => {
    aplicarVidrio(activo);
    try { localStorage.setItem(CLAVE_VIDRIO, activo ? 'si' : 'no'); } catch { /* almacenamiento no disponible */ }
  }, [activo]);
  const alternar = useCallback(() => setActivo((v) => !v), []);
  return [activo, alternar];
}
