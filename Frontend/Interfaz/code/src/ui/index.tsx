import { useEffect, useId, useRef, type CSSProperties, type ReactNode, type ButtonHTMLAttributes, type InputHTMLAttributes, type TextareaHTMLAttributes } from 'react';
import { acento, acentos, alphaCortes, colors, estados, font, motion, radius, size, space, web } from '@/theme/tokens';
import type { AcentoCurso, EstadoCalificacion } from '@/types';
import { ETIQUETA_ESTADO, diaMes } from '@/domain';
import { conOnda } from './onda';

export { lanzarOnda, conOnda } from './onda';

export const entrada = (i = 0): CSSProperties => ({ animation: `au-up ${motion.entrada}ms ${motion.easing} both`, animationDelay: `${80 + i * motion.escalonado}ms` });

const glass: CSSProperties = {
  background: colors.superficie,
  border: `1px solid ${colors.linea}`,
  borderRadius: radius.xl,
  backdropFilter: web.blur,
  WebkitBackdropFilter: web.blur,
  boxShadow: web.sombraSuave,
};

export function Panel({ children, style, padding = space[5], indice, as: Tag = 'section' }: { children: ReactNode; style?: CSSProperties; padding?: number; indice?: number; as?: 'section' | 'div' }) {
  return <Tag style={{ ...glass, padding, display: 'flex', flexDirection: 'column', gap: space[3], ...(indice != null ? entrada(indice) : null), ...style }}>{children}</Tag>;
}

export function Fila({ children, onClick, style }: { children: ReactNode; onClick?: () => void; style?: CSSProperties }) {
  return (
    <button type="button" onClick={onClick} className="au-hover-row" style={{ display: 'flex', alignItems: 'center', gap: 14, width: '100%', textAlign: 'left', background: colors.fila, border: `1px solid ${colors.linea}`, borderRadius: radius.lg, padding: '10px 14px', minHeight: 68, color: colors.texto, ...style }}>
      {children}
    </button>
  );
}

export function Tarjeta({ children, onClick, indice = 0 }: { children: ReactNode; onClick?: () => void; indice?: number }) {
  return (
    <button type="button" onClick={onClick} className="au-hover-lift" style={{ ...glass, padding: 18, display: 'flex', flexDirection: 'column', gap: space[4], textAlign: 'left', color: colors.texto, ...entrada(indice) }}>
      {children}
    </button>
  );
}

export function Chip({ estado }: { estado: EstadoCalificacion }) {
  const e = estados[estado];
  return <span style={{ display: 'inline-flex', alignItems: 'center', height: 26, padding: '0 12px', borderRadius: radius.pill, fontSize: font.size.sm, fontWeight: font.weight.semibold, whiteSpace: 'nowrap', background: e.fondo, color: e.texto }}>{ETIQUETA_ESTADO[estado]}</span>;
}

export function Monograma({ texto, acento: a, grande }: { texto: string; acento: AcentoCurso; grande?: boolean }) {
  const s = grande ? size.monogramaGrande : size.monograma;
  return <div style={{ width: s, height: s, flex: 'none', borderRadius: grande ? 14 : 11, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: grande ? 16 : 13, fontWeight: font.weight.bold, letterSpacing: '0.04em', background: acento(a, 0.11), color: acentos[a].texto, boxShadow: `inset 0 0 0 1px ${acento(a, 0.14)}` }}>{texto}</div>;
}

export function FechaTile({ fecha, grande }: { fecha: string; grande?: boolean }) {
  const { dia, mes } = diaMes(fecha);
  const s = grande ? 60 : 48;
  return (
    <div style={{ width: s, height: s, flex: 'none', borderRadius: 11, background: colors.fondoProfundo, border: `1px solid ${colors.linea}`, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', lineHeight: 1 }}>
      <span style={{ fontSize: grande ? 22 : 18, fontWeight: font.weight.bold }}>{dia}</span>
      <span style={{ fontSize: font.size.xxs, fontWeight: font.weight.semibold, letterSpacing: '0.08em', color: colors.textoTenue, marginTop: 3 }}>{mes}</span>
    </div>
  );
}

/** Barra de progreso con llenado progresivo al montar (`retardo` en ms para escalonar varias barras). */
export function Barra({ valor, color, alto = 6, max, retardo = 120, etiqueta }: { valor: number; color: string; alto?: number; max?: number; retardo?: number; etiqueta?: string }) {
  const v = Math.max(0, Math.min(100, valor));
  return (
    <div role="progressbar" aria-valuemin={0} aria-valuemax={100} aria-valuenow={Math.round(v)} aria-label={etiqueta} style={{ height: alto, borderRadius: alto / 2, background: colors.linea, overflow: 'hidden', maxWidth: max }}>
      <div className="au-barra-relleno" style={{ height: '100%', width: `${v}%`, background: color, borderRadius: alto / 2, transition: `width ${motion.base}ms ${motion.easing}`, ['--au-retardo' as string]: `${retardo}ms` } as CSSProperties} />
    </div>
  );
}

export function BarraCortes({ pesos, acento: a, etiquetas }: { pesos: number[]; acento: AcentoCurso; etiquetas?: Array<{ izquierda: string; derecha?: string }> }) {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: pesos.map((p) => `${p}fr`).join(' '), gap: 6 }}>
      {pesos.map((_, i) => (
        <div key={i} style={{ display: 'flex', flexDirection: 'column', gap: space[2], minWidth: 0 }}>
          <div style={{ height: 6, borderRadius: 3, background: colors.linea, overflow: 'hidden' }}>
            <div className="au-barra-relleno" style={{ height: '100%', borderRadius: 3, background: acento(a, alphaCortes[i]), ['--au-retardo' as string]: `${160 + i * 140}ms` } as CSSProperties} />
          </div>
          {etiquetas && (
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: 6, fontSize: font.size.sm, color: colors.textoTenue }}>
              <span style={{ whiteSpace: 'nowrap' }}>{etiquetas[i].izquierda}</span>
              <strong style={{ color: colors.texto, fontWeight: font.weight.semibold }}>{etiquetas[i].derecha}</strong>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

type Variante = 'marca' | 'claro' | 'contorno' | 'texto';
export function Boton({ variante = 'marca', style, className, onPointerDown, ...p }: ButtonHTMLAttributes<HTMLButtonElement> & { variante?: Variante }) {
  // backgroundColor (no `background`) para que el hover de global.css pueda sobrescribirlo.
  const v: Record<Variante, CSSProperties> = {
    marca: { backgroundColor: colors.marca, color: colors.sobreMarca, border: 0, boxShadow: web.sombraMarca },
    claro: { backgroundColor: colors.blanco, color: colors.texto, border: `1px solid ${colors.linea}`, fontWeight: font.weight.bold, boxShadow: web.sombraSuave },
    contorno: { backgroundColor: 'transparent', color: colors.texto, border: `1px solid ${colors.lineaFuerte}` },
    texto: { backgroundColor: 'transparent', color: colors.marcaTexto, border: 0, padding: 0, height: 'auto' },
  };
  const conEfecto = variante !== 'texto';
  return <button {...p} data-onda={variante === 'marca' ? 'clara' : undefined} onPointerDown={conEfecto ? conOnda(onPointerDown) : onPointerDown} className={[conEfecto && 'au-onda-host', `au-boton--${variante}`, className].filter(Boolean).join(' ')} style={{ height: size.toque, padding: '0 18px', borderRadius: radius.lg, fontSize: font.size.base, fontWeight: font.weight.semibold, opacity: p.disabled ? 0.5 : 1, ...v[variante], ...style }} />;
}

const campoBase: CSSProperties = { height: size.toque, padding: '0 12px', border: `1px solid ${colors.lineaFuerte}`, borderRadius: radius.md, background: colors.campo, color: colors.texto, fontSize: font.size.lg, width: '100%', transition: `border-color ${motion.rapido}ms ${motion.easing}, box-shadow ${motion.rapido}ms ${motion.easing}` };

export function Campo({ etiqueta, error, style, ...p }: InputHTMLAttributes<HTMLInputElement> & { etiqueta: string; error?: string }) {
  return (
    <label style={{ display: 'flex', flexDirection: 'column', gap: 6, fontSize: font.size.sm, fontWeight: font.weight.semibold, letterSpacing: '0.04em', color: colors.textoMedio, ...style }}>
      {etiqueta}
      <input {...p} aria-invalid={!!error} className="au-campo" style={{ ...campoBase, borderColor: error ? colors.error : colors.lineaFuerte, letterSpacing: 0 }} />
      {error && <span style={{ color: colors.error, fontWeight: font.weight.medium, letterSpacing: 0 }}>{error}</span>}
    </label>
  );
}

export function AreaTexto({ etiqueta, ...p }: TextareaHTMLAttributes<HTMLTextAreaElement> & { etiqueta: string }) {
  return (
    <label style={{ display: 'flex', flexDirection: 'column', gap: 6, fontSize: font.size.xs, fontWeight: font.weight.semibold, letterSpacing: '0.06em', color: colors.textoTenue, flex: 1, minWidth: 0 }}>
      {etiqueta}
      <textarea {...p} className="au-campo" style={{ ...campoBase, height: 'auto', minHeight: 48, padding: '10px 12px', fontSize: font.size.base, resize: 'vertical', letterSpacing: 0 }} />
    </label>
  );
}

export function CampoPorcentaje({ valor, onCambio, etiqueta, ancho }: { valor: string; onCambio: (v: string) => void; etiqueta?: string; ancho?: number }) {
  const caja = (
    <div style={{ display: 'flex', alignItems: 'center', border: `1px solid ${colors.lineaFuerte}`, borderRadius: radius.md, height: 46, padding: '0 12px', gap: 4, background: colors.campo, width: ancho, flex: ancho ? 'none' : undefined }}>
      <input value={valor} onChange={(e) => onCambio(e.target.value)} inputMode="numeric" aria-label={etiqueta} style={{ border: 0, width: '100%', minWidth: 0, background: 'transparent', fontSize: 18, fontWeight: font.weight.semibold, color: colors.texto }} />
      <span style={{ color: colors.textoTenue }}>%</span>
    </div>
  );
  if (!etiqueta) return caja;
  return <label style={{ display: 'flex', flexDirection: 'column', gap: 6, fontSize: font.size.xs, fontWeight: font.weight.semibold, letterSpacing: '0.06em', color: colors.textoTenue }}>{etiqueta}{caja}</label>;
}

export function Pestanas<T extends string>({ opciones, valor, onCambio }: { opciones: Array<{ id: T; etiqueta: string }>; valor: T; onCambio: (v: T) => void }) {
  return (
    <div role="tablist" aria-label="Secciones del curso" style={{ alignSelf: 'flex-start', maxWidth: '100%', display: 'flex', gap: 4, padding: 4, borderRadius: 14, background: colors.fondoProfundo, border: `1px solid ${colors.linea}`, overflowX: 'auto' }}>
      {opciones.map((o) => {
        const on = o.id === valor;
        return <button key={o.id} role="tab" aria-selected={on} tabIndex={on ? 0 : -1} onKeyDown={e => {
          const i = opciones.findIndex(x => x.id === valor);
          const siguiente = e.key === 'ArrowRight' ? (i + 1) % opciones.length : e.key === 'ArrowLeft' ? (i - 1 + opciones.length) % opciones.length : e.key === 'Home' ? 0 : e.key === 'End' ? opciones.length - 1 : -1;
          if (siguiente < 0) return;
          e.preventDefault(); onCambio(opciones[siguiente].id);
          const botones = e.currentTarget.parentElement?.querySelectorAll<HTMLButtonElement>('[role="tab"]'); botones?.[siguiente].focus();
        }} onPointerDown={conOnda()} data-onda={on ? 'clara' : undefined} className="au-onda-host" onClick={() => onCambio(o.id)} style={{ height: 40, padding: '0 16px', border: 0, borderRadius: radius.md, fontSize: font.size.base, fontWeight: font.weight.semibold, whiteSpace: 'nowrap', background: on ? colors.marca : 'transparent', color: on ? colors.sobreMarca : colors.textoMedio, boxShadow: on ? web.sombraMarca : 'none' }}>{o.etiqueta}</button>;
      })}
    </div>
  );
}

export function TituloPagina({ children, wide, descripcion }: { children: ReactNode; wide: boolean; descripcion?: string }) {
  return (
    <div className="au-titulo-pagina" style={{ display: 'flex', flexDirection: 'column', gap: space[2], ...entrada(0) }}>
      <h1 style={{ margin: 0, fontSize: wide ? font.size.h1 : 26, fontWeight: font.weight.light, letterSpacing: '0.06em', textTransform: 'uppercase' }}>{children}</h1>
      {descripcion && <span style={{ fontSize: font.size.lg, color: colors.textoMedio, maxWidth: 640, textWrap: 'pretty' } as CSSProperties}>{descripcion}</span>}
    </div>
  );
}

export function Cargando({ filas = 3 }: { filas?: number }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: space[3] }} aria-busy="true" aria-label="Cargando información">
      {Array.from({ length: filas }, (_, i) => <div key={i} style={{ height: 68, borderRadius: radius.lg, background: colors.linea, animation: 'au-pulse 1.4s ease-in-out infinite', animationDelay: `${i * 120}ms` }} />)}
    </div>
  );
}

export function ErrorEstado({ error, onReintentar }: { error: unknown; onReintentar?: () => void }) {
  return (
    <Panel style={{ alignItems: 'flex-start' }}>
      <strong>No se pudo cargar la información.</strong>
      <span style={{ color: colors.textoTenue, fontSize: font.size.base }}>{error instanceof Error ? error.message : 'Intenta de nuevo.'}</span>
      {onReintentar && <Boton variante="contorno" onClick={onReintentar}>Reintentar</Boton>}
    </Panel>
  );
}

export function Vacio({ children }: { children: ReactNode }) {
  return <div style={{ padding: '40px 16px', textAlign: 'center', color: colors.textoTenue, fontSize: font.size.base }}>{children}</div>;
}

export function Chevron() {
  return <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke={colors.textoApagado} strokeWidth="2" strokeLinecap="round" style={{ flex: 'none' }}><path d="m9 6 6 6-6 6" /></svg>;
}

export const icono = {
  inicio: 'M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6h-6v6H4a1 1 0 0 1-1-1z',
  cursos: 'M12 6.5C10 5 7 4.5 4 5v14c3-.5 6 0 8 1.5 2-1.5 5-2 8-1.5V5c-3-.5-6 0-8 1.5zM12 6.5v14',
  notas: 'M5 20v-8M12 20V5M19 20v-5',
  salir: 'M14 4h5v16h-5M10 8l-4 4 4 4M6 12h10',
  atras: 'm15 6-6 6 6 6',
  subir: 'M12 16V5M7 10l5-5 5 5M5 19h14',
  calendario: 'M4 7a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2zM4 10h16M9 3v4M15 3v4',
  accesibilidad: 'M10 4.5a2 2 0 1 0 4 0a2 2 0 1 0-4 0M4.5 8.5 12 10l7.5-1.5M12 10v4.5M8.5 21l3.5-6.5 3.5 6.5',
  cerrar: 'M6 6l12 12M18 6 6 18',
  vidrio: 'M4 6a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2zM4 15l4.5-4.5a1.5 1.5 0 0 1 2 0L16 16M14 14l1.5-1.5a1.5 1.5 0 0 1 2 0L20 15M15 8.5h.01',
  desplegar: 'm6 9 6 6 6-6',
} as const;

export function Icono({ d, tam = 20 }: { d: string; tam?: number }) {
  return <svg width={tam} height={tam} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={d} /></svg>;
}

/**
 * Ventana emergente sobre un overlay blanco translúcido con desenfoque.
 * `saliendo` reproduce la animación de salida antes de desmontar (el padre controla el tiempo).
 */
export function Modal({ children, onCerrar, titulo, saliendo, ancho = 400 }: { children: ReactNode; onCerrar: () => void; titulo: string; saliendo?: boolean; ancho?: number }) {
  const id = useId();
  const caja = useRef<HTMLDivElement>(null);
  const cerrar = useRef(onCerrar);
  cerrar.current = onCerrar;
  useEffect(() => {
    const previo = document.activeElement as HTMLElement | null;
    const tecla = (e: KeyboardEvent) => {
      if (e.key === 'Escape') { e.preventDefault(); cerrar.current(); return; }
      if (e.key !== 'Tab' || !caja.current) return;
      const f = caja.current.querySelectorAll<HTMLElement>('button:not([disabled]),input:not([disabled]),select,textarea,a[href],[tabindex]:not([tabindex="-1"])');
      if (!f.length) return;
      const primero = f[0], ultimo = f[f.length - 1];
      if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo.focus(); }
      else if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero.focus(); }
    };
    document.addEventListener('keydown', tecla);
    return () => { document.removeEventListener('keydown', tecla); previo?.focus?.(); };
  }, []);
  return (
    <div className="au-overlay" data-saliendo={saliendo ? 'true' : undefined} onMouseDown={(e) => { if (e.target === e.currentTarget) onCerrar(); }}>
      <div ref={caja} role="dialog" aria-modal="true" aria-label={titulo} id={id} className="au-modal" style={{ maxWidth: ancho }}>
        {children}
      </div>
    </div>
  );
}
