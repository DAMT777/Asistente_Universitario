import { Fragment, useEffect, useRef, useState, type CSSProperties, type KeyboardEvent, type ReactNode } from 'react';
import { useAsistente, useUsuario, type MensajeChat } from '@/hooks';
import { colors, font, motion, radius, space, web } from '@/theme/tokens';
import { Boton, Icono, icono, conOnda, entrada } from '@/ui';
import { ImportarNotas } from './ImportarNotas';
import { useToast } from '@/ui/Toast';

const MAX_CARACTERES = 1000;

const SUGERENCIAS = {
  estudiante: ['¿Cuáles son mis notas publicadas?', '¿Cuál es mi definitiva parcial?', '¿Qué entregas he subido?'],
  docente: ['¿Qué cursos tengo?', '¿Quiénes están inscritos?', 'Ponle 4.2 a Luis en el Taller 1'],
} as const;

export type EstadoVentana = 'cerrada' | 'abierta' | 'minimizada';

/**
 * Chat flotante del asistente, al estilo de una ventana de mensajería: se abre desde el botón de la barra
 * superior, se puede minimizar a una burbuja y cerrar. Vive en AppShell, así que la conversación sigue
 * aunque se cambie de página.
 */
export function VentanaAsistente({ estado, onCambiar }: { estado: EstadoVentana; onCambiar: (e: EstadoVentana) => void }) {
  const u = useUsuario();
  const chat = useAsistente();
  const [texto, setTexto] = useState('');
  const [sinLeer, setSinLeer] = useState(false);
  const lista = useRef<HTMLDivElement>(null);
  const campo = useRef<HTMLTextAreaElement>(null);
  const docente = u.rol === 'docente';
  const abierta = estado === 'abierta';
  const vistos = useRef(chat.mensajes.length);

  // Siempre muestra lo último que llegó.
  useEffect(() => {
    if (abierta) lista.current?.scrollTo({ top: lista.current.scrollHeight, behavior: 'smooth' });
  }, [chat.mensajes.length, chat.enviando, abierta]);

  // Punto rojo en la burbuja si llegó una respuesta mientras estaba minimizada.
  useEffect(() => {
    if (abierta) { vistos.current = chat.mensajes.length; setSinLeer(false); return; }
    if (chat.mensajes.length > vistos.current) setSinLeer(true);
  }, [chat.mensajes.length, abierta]);

  useEffect(() => { if (abierta) campo.current?.focus(); }, [abierta]);

  useEffect(() => {
    if (!abierta) return;
    const tecla = (e: globalThis.KeyboardEvent) => { if (e.key === 'Escape') onCambiar('minimizada'); };
    document.addEventListener('keydown', tecla);
    return () => document.removeEventListener('keydown', tecla);
  }, [abierta, onCambiar]);

  const enviar = async (valor = texto) => {
    if (!valor.trim() || chat.enviando) return;
    setTexto('');
    await chat.enviar(valor);
    campo.current?.focus();
  };

  const alTeclear = (e: KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey && !e.nativeEvent.isComposing) { e.preventDefault(); void enviar(); }
  };

  if (estado === 'cerrada') return null;

  if (estado === 'minimizada') {
    return (
      <button type="button" className="au-chat-burbuja au-onda-host" onPointerDown={conOnda()} onClick={() => onCambiar('abierta')} aria-label={sinLeer ? 'Abrir el asistente (hay una respuesta nueva)' : 'Abrir el asistente'} title="Asistente Sirius">
        <Icono d={icono.asistente} tam={24} />
        {sinLeer && <span className="au-chat-punto" aria-hidden="true" />}
      </button>
    );
  }

  const botonCabecera: CSSProperties = { width: 32, height: 32, border: 0, borderRadius: 8, background: 'transparent', color: colors.marcaTexto, display: 'flex', alignItems: 'center', justifyContent: 'center', flex: 'none' };

  return (
    <section className="au-chat" role="dialog" aria-label="Asistente Sirius">
      <header style={{ display: 'flex', alignItems: 'center', gap: space[3], padding: '10px 10px 10px 14px', borderBottom: `1px solid ${colors.linea}`, boxShadow: '0 1px 4px rgba(28,26,27,0.05)' }}>
        <div aria-hidden="true" style={{ position: 'relative', width: 36, height: 36, flex: 'none', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', background: `linear-gradient(135deg,${colors.marca},${colors.marcaHover})`, color: colors.sobreMarca }}>
          <Icono d={icono.asistente} tam={19} />
          <span style={{ position: 'absolute', right: -1, bottom: -1, width: 11, height: 11, borderRadius: '50%', background: colors.exito, border: `2px solid ${colors.superficieFuerte}` }} />
        </div>
        <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', lineHeight: 1.25 }}>
          <strong style={{ fontSize: font.size.lg, fontWeight: font.weight.semibold, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>Asistente Sirius</strong>
          <span style={{ fontSize: font.size.xxs, color: colors.textoTenue }}>{chat.enviando ? 'Escribiendo…' : docente ? 'Consultas y calificaciones' : 'Consulta tus notas'}</span>
        </div>
        {chat.mensajes.length > 0 && (
          <button type="button" className="au-chat-accion" onClick={chat.reiniciar} disabled={chat.enviando} aria-label="Nueva conversación" title="Nueva conversación" style={botonCabecera}>
            <Icono d={icono.reiniciar} tam={17} />
          </button>
        )}
        <button type="button" className="au-chat-accion" onClick={() => onCambiar('minimizada')} aria-label="Minimizar" title="Minimizar" style={botonCabecera}>
          <Icono d={icono.minimizar} tam={18} />
        </button>
        <button type="button" className="au-chat-accion" onClick={() => onCambiar('cerrada')} aria-label="Cerrar el asistente" title="Cerrar" style={botonCabecera}>
          <Icono d={icono.cerrar} tam={18} />
        </button>
      </header>

      <div ref={lista} role="log" aria-live="polite" aria-label="Conversación con el asistente" style={{ flex: 1, minHeight: 0, overflowY: 'auto', padding: space[4], display: 'flex', flexDirection: 'column', gap: space[3] }}>
        {chat.mensajes.length === 0
          ? <Bienvenida nombre={u.nombre} docente={docente} sugerencias={SUGERENCIAS[u.rol]} onElegir={(s) => void enviar(s)} />
          : chat.mensajes.map((m) => <Burbuja key={m.id} m={m} docente={docente} acciones={chat} />)}
        {chat.enviando && <Escribiendo />}
      </div>

      {docente && <ImportarNotas />}
      <form onSubmit={(e) => { e.preventDefault(); void enviar(); }} style={{ padding: '10px 12px 12px', display: 'flex', alignItems: 'flex-end', gap: space[2] }}>
        <textarea
          ref={campo}
          aria-label="Mensaje para el asistente"
          value={texto}
          onChange={(e) => setTexto(e.target.value.slice(0, MAX_CARACTERES))}
          onKeyDown={alTeclear}
          rows={1}
          placeholder="Escribe un mensaje…"
          className="au-chat-campo"
          style={{ flex: 1, minWidth: 0, resize: 'none', minHeight: 40, maxHeight: 120, padding: '9px 14px', border: `1px solid ${colors.linea}`, borderRadius: 20, background: colors.fondoProfundo, color: colors.texto, fontSize: font.size.base, fontFamily: 'inherit', lineHeight: 1.4, fieldSizing: 'content' } as CSSProperties}
        />
        <button type="submit" disabled={!texto.trim() || chat.enviando} aria-label="Enviar mensaje" className="au-chat-enviar" style={{ width: 40, height: 40, flex: 'none', border: 0, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', background: 'transparent', color: colors.marca, opacity: !texto.trim() || chat.enviando ? 0.4 : 1 }}>
          <Icono d={icono.enviar} tam={22} />
        </button>
      </form>
      <span style={{ fontSize: 11, color: colors.textoApagado, textAlign: 'center', padding: '0 12px 10px' }}>Usa inteligencia artificial y puede equivocarse.</span>
    </section>
  );
}

function Bienvenida({ nombre, docente, sugerencias, onElegir }: { nombre: string; docente: boolean; sugerencias: readonly string[]; onElegir: (s: string) => void }) {
  return (
    <div style={{ margin: 'auto', maxWidth: 320, display: 'flex', flexDirection: 'column', alignItems: 'center', gap: space[4], textAlign: 'center', ...entrada(2) }}>
      <div style={{ width: 56, height: 56, borderRadius: 18, display: 'flex', alignItems: 'center', justifyContent: 'center', background: colors.marcaSuave, color: colors.marcaTexto, border: `1px solid ${colors.lineaMarca}` }}>
        <Icono d={icono.asistente} tam={32} />
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: space[1] }}>
        <strong style={{ fontSize: font.size.xl, fontWeight: font.weight.semibold }}>Hola, {nombre.split(' ')[0]}</strong>
        <span style={{ color: colors.textoMedio, fontSize: font.size.base }}>{docente ? 'Pregunta por tus cursos o pide una nota. Nada se guarda hasta que lo confirmes.' : 'Pregunta por tus notas publicadas y tus entregas.'}</span>
      </div>
      <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'center', gap: space[2] }}>
        {sugerencias.map((s, i) => (
          <button key={s} type="button" onClick={() => onElegir(s)} onPointerDown={conOnda()} className="au-onda-host au-boton--claro"
            style={{ minHeight: 36, padding: '6px 14px', borderRadius: radius.pill, border: `1px solid ${colors.linea}`, backgroundColor: colors.blanco, color: colors.texto, fontSize: font.size.sm, fontWeight: font.weight.medium, boxShadow: web.sombraSuave, ...entrada(1 + i) }}>
            {s}
          </button>
        ))}
      </div>
    </div>
  );
}

function Burbuja({ m, docente, acciones }: { m: MensajeChat; docente: boolean; acciones: ReturnType<typeof useAsistente> }) {
  const propio = m.autor === 'usuario';
  const hora = new Date(m.fecha).toLocaleTimeString('es-CO', { hour: 'numeric', minute: '2-digit' });
  return (
    <div style={{ display: 'flex', gap: space[3], flexDirection: propio ? 'row-reverse' : 'row', alignItems: 'flex-end', animation: `au-up ${motion.base}ms ${motion.easing} both` }}>
      {!propio && (
        <div aria-hidden="true" style={{ width: 28, height: 28, flex: 'none', borderRadius: 9, display: 'flex', alignItems: 'center', justifyContent: 'center', background: m.error ? colors.fondoProfundo : colors.marcaSuave, color: m.error ? colors.textoTenue : colors.marcaTexto, border: `1px solid ${m.error ? colors.linea : colors.lineaMarca}` }}>
          <Icono d={icono.asistente} tam={17} />
        </div>
      )}
      <div style={{ maxWidth: '86%', display: 'flex', flexDirection: 'column', gap: space[2], alignItems: propio ? 'flex-end' : 'flex-start' }}>
        <div style={{
          padding: '8px 12px', fontSize: font.size.base, lineHeight: font.lineHeight.lectura, overflowWrap: 'anywhere',
          borderRadius: propio ? `${radius.xl}px ${radius.xl}px 4px ${radius.xl}px` : `${radius.xl}px ${radius.xl}px ${radius.xl}px 4px`,
          ...(propio
            ? { background: `linear-gradient(135deg,${colors.marca},${colors.marcaHover})`, color: colors.sobreMarca, boxShadow: web.sombraMarca }
            : m.error
              ? { background: colors.fondoProfundo, color: colors.error, border: `1px solid ${colors.linea}` }
              : { background: colors.blanco, color: colors.texto, border: `1px solid ${colors.linea}`, boxShadow: web.sombraSuave }),
        }}>
          {propio ? m.texto : <TextoFormateado texto={m.texto} />}
        </div>
        {m.accion && docente && <TarjetaAccion accion={m.accion} acciones={acciones} />}
        <span style={{ fontSize: font.size.xxs, color: colors.textoApagado }}>{propio ? 'Tú' : 'Asistente'} · {hora}</span>
      </div>
    </div>
  );
}

const ESTADO_ACCION = {
  pendiente: { etiqueta: 'Pendiente de confirmar', color: colors.aviso },
  confirmando: { etiqueta: 'Guardando…', color: colors.aviso },
  confirmada: { etiqueta: 'Guardada en borrador', color: colors.exito },
  cancelada: { etiqueta: 'Cancelada', color: colors.textoTenue },
  expirada: { etiqueta: 'Vencida', color: colors.textoTenue },
  fallida: { etiqueta: 'No se guardó', color: colors.error },
} as const;

function TarjetaAccion({ accion, acciones }: { accion: NonNullable<MensajeChat['accion']>; acciones: ReturnType<typeof useAsistente> }) {
  const restante = useCuentaRegresiva(accion.expiraEn, accion.estado === 'pendiente');
  const toast = useToast();
  const { marcarExpirada } = acciones;
  useEffect(() => { if (accion.estado === 'pendiente' && restante === 0) marcarExpirada(accion.id); }, [accion.estado, accion.id, restante, marcarExpirada]);

  const e = ESTADO_ACCION[accion.estado];
  const activa = accion.estado === 'pendiente' || accion.estado === 'confirmando';
  const mm = Math.floor(restante / 60), ss = String(restante % 60).padStart(2, '0');

  return (
    <div role="group" aria-label="Calificación propuesta" style={{ width: '100%', borderRadius: radius.lg, border: `1px solid ${activa ? colors.lineaMarca : colors.linea}`, background: colors.superficieFuerte, boxShadow: web.sombraSuave, overflow: 'hidden' }}>
      <div style={{ height: 3, background: activa ? `linear-gradient(90deg, ${colors.marcaHover}, ${colors.marca} 50%, ${colors.marcaBrillante})` : colors.linea }} />
      <div style={{ padding: space[3], display: 'flex', flexDirection: 'column', gap: space[2] }}>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: space[2], flexWrap: 'wrap' }}>
          <span style={{ fontSize: font.size.xs, fontWeight: font.weight.semibold, letterSpacing: '0.06em', textTransform: 'uppercase', color: colors.textoTenue }}>Calificación propuesta</span>
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6, fontSize: font.size.sm, fontWeight: font.weight.semibold, color: e.color }}>
            <span style={{ width: 8, height: 8, borderRadius: '50%', background: e.color }} />{e.etiqueta}
          </span>
        </div>
        <span style={{ fontSize: font.size.base, fontWeight: font.weight.medium, lineHeight: font.lineHeight.normal }}>{accion.resumen}</span>
        {accion.estado === 'pendiente' && (
          <span style={{ fontSize: font.size.sm, color: restante < 60 ? colors.error : colors.textoTenue }} aria-live="off">Vence en {mm}:{ss}</span>
        )}
        {accion.detalle && <span role="status" style={{ fontSize: font.size.sm, color: accion.estado === 'fallida' ? colors.error : colors.textoMedio }}>{accion.detalle}</span>}
        {activa && (
          <div style={{ display: 'flex', gap: space[2], flexWrap: 'wrap' }}>
            <Boton disabled={accion.estado === 'confirmando'} onClick={async () => { if (await acciones.confirmar(accion.id)) toast('Nota guardada en borrador'); }} style={{ flex: 1, minWidth: 0, height: 38, padding: '0 10px' }}>
              {accion.estado === 'confirmando' ? 'Guardando…' : 'Confirmar'}
            </Boton>
            <Boton variante="contorno" disabled={accion.estado === 'confirmando'} onClick={() => void acciones.cancelar(accion.id)} style={{ flex: 1, minWidth: 0, height: 38, padding: '0 10px' }}>Cancelar</Boton>
          </div>
        )}
      </div>
    </div>
  );
}

function useCuentaRegresiva(hasta: string, activa: boolean) {
  const calcular = () => Math.max(0, Math.round((new Date(hasta).getTime() - Date.now()) / 1000));
  const [s, setS] = useState(calcular);
  useEffect(() => {
    if (!activa) return;
    setS(calcular());
    const t = setInterval(() => setS(calcular()), 1000);
    return () => clearInterval(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasta, activa]);
  return s;
}

function Escribiendo() {
  return (
    <div role="status" aria-label="El asistente está escribiendo" style={{ display: 'flex', gap: space[3], alignItems: 'center' }}>
      <div aria-hidden="true" style={{ width: 28, height: 28, borderRadius: 9, display: 'flex', alignItems: 'center', justifyContent: 'center', background: colors.marcaSuave, color: colors.marcaTexto, border: `1px solid ${colors.lineaMarca}` }}>
        <Icono d={icono.asistente} tam={17} />
      </div>
      <div style={{ display: 'flex', gap: 5, padding: '12px 14px', borderRadius: radius.xl, background: colors.blanco, border: `1px solid ${colors.linea}` }}>
        {[0, 1, 2].map(i => <span key={i} style={{ width: 7, height: 7, borderRadius: '50%', background: colors.textoApagado, animation: 'au-pulse 1.2s ease-in-out infinite', animationDelay: `${i * 160}ms` }} />)}
      </div>
    </div>
  );
}

/**
 * Formato mínimo de la respuesta del modelo: párrafos, listas con "-" o "1." y **negrita** / *cursiva*.
 * Se arma con elementos React (nunca HTML crudo), así que el texto no puede inyectar marcado.
 */
function TextoFormateado({ texto }: { texto: string }) {
  const bloques: ReactNode[] = [];
  let lista: { ordenada: boolean; items: string[] } | null = null;
  const cerrarLista = () => {
    if (!lista) return;
    const Tag = lista.ordenada ? 'ol' : 'ul';
    bloques.push(<Tag key={bloques.length} style={{ margin: 0, paddingLeft: 20, display: 'flex', flexDirection: 'column', gap: 2 }}>{lista.items.map((it, i) => <li key={i}>{enLinea(it)}</li>)}</Tag>);
    lista = null;
  };
  for (const linea of texto.replace(/\u202f/g, ' ').split('\n')) {
    const vineta = /^\s*[-*•]\s+(.*)$/.exec(linea);
    const numero = /^\s*\d+[.)]\s+(.*)$/.exec(linea);
    if (vineta || numero) {
      const ordenada = !!numero;
      if (lista && lista.ordenada !== ordenada) cerrarLista();
      lista ??= { ordenada, items: [] };
      lista.items.push((vineta ?? numero)![1]);
      continue;
    }
    cerrarLista();
    if (linea.trim()) bloques.push(<p key={bloques.length} style={{ margin: 0 }}>{enLinea(linea.replace(/^#+\s*/, ''))}</p>);
  }
  cerrarLista();
  return <div style={{ display: 'flex', flexDirection: 'column', gap: space[2] }}>{bloques}</div>;
}

function enLinea(texto: string): ReactNode {
  return texto.split(/(\*\*[^*]+\*\*|\*[^*\s][^*]*\*)/g).map((parte, i) => {
    if (/^\*\*[^*]+\*\*$/.test(parte)) return <strong key={i} style={{ fontWeight: font.weight.semibold }}>{parte.slice(2, -2)}</strong>;
    if (/^\*[^*]+\*$/.test(parte)) return <em key={i}>{parte.slice(1, -1)}</em>;
    return <Fragment key={i}>{parte}</Fragment>;
  });
}
