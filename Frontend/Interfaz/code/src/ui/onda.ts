import type { PointerEvent as ReactPointerEvent } from 'react';

/**
 * Efecto onda (ripple). Dibuja un círculo que se expande desde el punto de contacto.
 * El elemento debe tener la clase `au-onda-host` (ver global.css). Solo web:
 * en React Native se reemplaza por `android_ripple` / Pressable.
 */
export function lanzarOnda(e: ReactPointerEvent<HTMLElement>) {
  const host = e.currentTarget;
  if ((host as HTMLButtonElement).disabled) return;
  const r = host.getBoundingClientRect();
  const diametro = Math.hypot(Math.max(e.clientX - r.left, r.right - e.clientX), Math.max(e.clientY - r.top, r.bottom - e.clientY)) * 2;
  const onda = document.createElement('span');
  onda.className = 'au-onda';
  onda.style.width = onda.style.height = `${diametro}px`;
  onda.style.left = `${e.clientX - r.left}px`;
  onda.style.top = `${e.clientY - r.top}px`;
  onda.addEventListener('animationend', () => onda.remove(), { once: true });
  host.appendChild(onda);
  // Respaldo por si la animación está desactivada (movimiento reducido).
  window.setTimeout(() => onda.remove(), 900);
}

/** Combina el manejador propio de onPointerDown con la onda. */
export const conOnda = <T extends HTMLElement>(propio?: (e: ReactPointerEvent<T>) => void) => (e: ReactPointerEvent<T>) => {
  lanzarOnda(e as unknown as ReactPointerEvent<HTMLElement>);
  propio?.(e);
};
