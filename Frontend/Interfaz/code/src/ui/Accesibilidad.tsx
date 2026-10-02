import { useEffect, useId, useRef, useState, type CSSProperties } from 'react';
import { colors, font } from '@/theme/tokens';
import { Icono, icono } from './index';
import { conOnda } from './onda';

type Prefs = { escala: 1 | 1.12 | 1.25; oscuro: boolean; contraste: boolean; enlaces: boolean; espaciado: boolean; movimiento: boolean };
const INICIAL: Prefs = { escala: 1, oscuro: false, contraste: false, enlaces: false, espaciado: false, movimiento: false };
const CLAVE = 'aula.accesibilidad';
const TAMANOS: Array<{ v: Prefs['escala']; etiqueta: string; nombre: string }> = [
  { v: 1, etiqueta: 'A', nombre: 'Texto normal' },
  { v: 1.12, etiqueta: 'A+', nombre: 'Texto grande' },
  { v: 1.25, etiqueta: 'A++', nombre: 'Texto muy grande' },
];

function leer(): Prefs {
  try { return { ...INICIAL, ...JSON.parse(localStorage.getItem(CLAVE) ?? '{}') }; } catch { return INICIAL; }
}

/** Aplica las preferencias como atributos en <html>; global.css hace el resto. */
function aplicar(p: Prefs) {
  const h = document.documentElement;
  h.style.setProperty('--au-escala', String(p.escala));
  const attr = (n: string, on: boolean, v: string) => (on ? h.setAttribute(n, v) : h.removeAttribute(n));
  attr('data-au-tema', p.oscuro, 'oscuro');
  attr('data-au-contraste', p.contraste, 'alto');
  attr('data-au-enlaces', p.enlaces, 'subrayados');
  attr('data-au-espaciado', p.espaciado, 'amplio');
  attr('data-au-movimiento', p.movimiento, 'reducido');
}

/** Aplica las preferencias guardadas antes del primer render (evita que el modo nocturno parpadee). */
export function aplicarAccesibilidadGuardada() {
  aplicar(leer());
}

/** Botón flotante de accesibilidad con menú desplegable. Solo web. */
export function Accesibilidad() {
  const [abierto, setAbierto] = useState(false);
  const [p, setP] = useState<Prefs>(leer);
  const id = useId();
  const raiz = useRef<HTMLDivElement>(null);
  const boton = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    aplicar(p);
    try { localStorage.setItem(CLAVE, JSON.stringify(p)); } catch { /* almacenamiento no disponible */ }
  }, [p]);

  useEffect(() => {
    if (!abierto) return;
    const fuera = (e: PointerEvent) => { if (!raiz.current?.contains(e.target as Node)) setAbierto(false); };
    const tecla = (e: KeyboardEvent) => { if (e.key === 'Escape') { setAbierto(false); boton.current?.focus(); } };
    document.addEventListener('pointerdown', fuera);
    document.addEventListener('keydown', tecla);
    return () => { document.removeEventListener('pointerdown', fuera); document.removeEventListener('keydown', tecla); };
  }, [abierto]);

  /**
   * Cambiar entre modo claro y nocturno pasa como una ola: al oscurecer baja desde arriba y al aclarar
   * sube desde abajo (View Transitions + máscara de global.css). Sin soporte del navegador, o con
   * "Reducir animaciones", el cambio es inmediato.
   */
  const cambiar = (siguiente: Prefs) => {
    const raizHtml = document.documentElement;
    const sinMovimiento = siguiente.movimiento || window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const doc = document as Document & { startViewTransition?: (cb: () => void) => { finished: Promise<void> } };
    if (siguiente.oscuro !== p.oscuro && !sinMovimiento && typeof doc.startViewTransition === 'function') {
      raizHtml.setAttribute('data-au-ola', siguiente.oscuro ? 'baja' : 'sube');
      doc.startViewTransition(() => aplicar(siguiente)).finished.finally(() => raizHtml.removeAttribute('data-au-ola'));
    }
    setP(siguiente);
  };

  const opciones: Array<{ k: Exclude<keyof Prefs, 'escala'>; etiqueta: string }> = [
    { k: 'oscuro', etiqueta: 'Modo nocturno' },
    { k: 'contraste', etiqueta: 'Alto contraste' },
    { k: 'enlaces', etiqueta: 'Subrayar enlaces' },
    { k: 'espaciado', etiqueta: 'Espaciado de lectura' },
    { k: 'movimiento', etiqueta: 'Reducir animaciones' },
  ];
  const item = (i: number) => ({ ['--i' as string]: i }) as CSSProperties;

  return (
    <div ref={raiz} className="au-a11y">
      {abierto && (
        <div id={id} role="dialog" aria-label="Opciones de accesibilidad" className="au-a11y-panel">
          <div className="au-a11y-item" style={{ ...item(0), display: 'flex', flexDirection: 'column', gap: 2 }}>
            <strong style={{ fontSize: font.size.lg, color: colors.texto }}>Accesibilidad</strong>
            <span style={{ fontSize: font.size.xxs, color: colors.textoTenue }}>Se guarda en este navegador</span>
          </div>
          <div className="au-a11y-item" style={item(1)}>
            <span style={{ display: 'block', fontSize: font.size.xs, fontWeight: font.weight.semibold, color: colors.textoTenue, marginBottom: 6 }}>Tamaño del texto</span>
            <div className="au-a11y-tamanos" role="group" aria-label="Tamaño del texto">
              {TAMANOS.map((t) => (
                <button key={t.v} type="button" aria-label={t.nombre} aria-pressed={p.escala === t.v} className="au-onda-host" onPointerDown={conOnda()} onClick={() => setP({ ...p, escala: t.v })}>{t.etiqueta}</button>
              ))}
            </div>
          </div>
          {opciones.map((o, i) => (
            <div key={o.k} className="au-a11y-item" style={item(i + 2)}>
              <button type="button" aria-pressed={p[o.k]} className="au-a11y-opcion au-onda-host" onPointerDown={conOnda()} onClick={() => cambiar({ ...p, [o.k]: !p[o.k] })}>
                {o.etiqueta}<span className="au-a11y-switch" aria-hidden="true" />
              </button>
            </div>
          ))}
          <div className="au-a11y-item" style={item(opciones.length + 2)}>
            <button type="button" onClick={() => cambiar(INICIAL)} style={{ width: '100%', minHeight: 40, border: 0, background: 'none', color: colors.marcaTexto, fontSize: font.size.base, fontWeight: font.weight.semibold }}>Restablecer</button>
          </div>
        </div>
      )}
      <button ref={boton} type="button" aria-label="Opciones de accesibilidad" aria-expanded={abierto} aria-controls={abierto ? id : undefined}
        className="au-a11y-boton au-onda-host" data-onda={abierto ? 'clara' : undefined} onPointerDown={conOnda()} onClick={() => setAbierto((v) => !v)}>
        <Icono d={abierto ? icono.cerrar : icono.accesibilidad} tam={24} />
      </button>
    </div>
  );
}
