import { useEffect, useId, useRef, useState, type CSSProperties } from 'react';
import { useNavigate } from 'react-router-dom';
import type { Notificacion, Rol, TipoNotificacion } from '@/types';
import { useNotificaciones, useUsuario } from '@/hooks';
import { fechaCompleta, haceCuanto } from '@/domain';
import { colors, font, radius } from '@/theme/tokens';
import { Boton, Cargando, Icono, icono, conOnda } from '@/ui';
import { rutaNotificacion } from './rutaNotificacion';

const ESTILO_TIPO: Record<TipoNotificacion, { d: string; color: string; etiqueta: string }> = {
  nueva_actividad: { d: icono.tarea, color: colors.marca, etiqueta: 'Nueva actividad' },
  curso_actualizado: { d: icono.editar, color: colors.aviso, etiqueta: 'Cambio en el curso' },
  nota_publicada: { d: icono.nota, color: colors.exito, etiqueta: 'Nota publicada' },
};


const contador = (n: number) => (n > 9 ? '9+' : String(n));

/**
 * Botón de campana de la barra superior. Muestra cuántas notificaciones hay sin leer y, al pulsarlo, un panel con
 * las más recientes. Pulsar un aviso lo marca como leído y abre lo que lo originó.
 */
export function CampanaNotificaciones({ estiloBoton }: { estiloBoton: CSSProperties }) {
  const u = useUsuario();
  const nav = useNavigate();
  const { noLeidas, notificaciones, isLoading, isError, error, refetch, marcarLeida, marcarTodas, marcandoTodas } = useNotificaciones();
  const [abierta, setAbierta] = useState(false);
  const raiz = useRef<HTMLDivElement>(null);
  const boton = useRef<HTMLButtonElement>(null);
  const idPanel = useId();

  // Recuerda cuántas había para sacudir la campana solo cuando llega una nueva.
  const [sacudir, setSacudir] = useState(false);
  const previas = useRef(noLeidas);
  useEffect(() => {
    if (noLeidas > previas.current) setSacudir(true);
    previas.current = noLeidas;
  }, [noLeidas]);

  useEffect(() => {
    if (!abierta) return;
    const fuera = (e: PointerEvent) => { if (!raiz.current?.contains(e.target as Node)) setAbierta(false); };
    const tecla = (e: KeyboardEvent) => { if (e.key === 'Escape') { setAbierta(false); boton.current?.focus(); } };
    document.addEventListener('pointerdown', fuera);
    document.addEventListener('keydown', tecla);
    return () => { document.removeEventListener('pointerdown', fuera); document.removeEventListener('keydown', tecla); };
  }, [abierta]);

  const alternar = () => {
    if (!abierta) void refetch();
    setAbierta(!abierta);
  };

  const abrir = (n: Notificacion) => {
    if (!n.leida) marcarLeida(n.id);
    setAbierta(false);
    nav(rutaNotificacion(n, u.rol));
  };

  const etiqueta = noLeidas > 0 ? `Notificaciones, ${noLeidas} sin leer` : 'Notificaciones';

  return (
    <div ref={raiz} style={{ position: 'relative' }}>
      <button
        ref={boton} type="button" aria-label={etiqueta} title={etiqueta} aria-haspopup="dialog" aria-expanded={abierta} aria-controls={abierta ? idPanel : undefined} aria-pressed={abierta}
        onPointerDown={conOnda()} onClick={alternar} onAnimationEnd={() => setSacudir(false)}
        className="au-onda-host au-boton--contorno au-boton-activo au-campana-boton" data-sacudir={sacudir || undefined} style={estiloBoton}
      >
        <Icono d={icono.campana} tam={18} />
      </button>
      {noLeidas > 0 && <span aria-hidden="true" className="au-campana-insignia">{contador(noLeidas)}</span>}

      {abierta && (
        <div id={idPanel} role="dialog" aria-label="Notificaciones" className="au-campana-panel">
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, padding: '16px 16px 12px', borderBottom: `1px solid ${colors.linea}` }}>
            <div style={{ display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
              <h2 style={{ margin: 0, fontSize: 16, fontWeight: font.weight.bold }}>Notificaciones</h2>
              <span style={{ fontSize: font.size.sm, color: colors.textoTenue }}>
                {noLeidas === 0 ? 'Estás al día' : noLeidas === 1 ? '1 sin leer' : `${noLeidas} sin leer`}
              </span>
            </div>
            {noLeidas > 0 && (
              <Boton variante="texto" onClick={marcarTodas} disabled={marcandoTodas} style={{ fontSize: font.size.sm, whiteSpace: 'nowrap' }}>
                Marcar todas como leídas
              </Boton>
            )}
          </div>

          <div style={{ overflowY: 'auto', flex: 1, minHeight: 0 }}>
            {isLoading ? (
              <div style={{ padding: 16 }}><Cargando filas={3} /></div>
            ) : isError ? (
              <div role="alert" style={{ padding: '28px 20px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 12, textAlign: 'center' }}>
                <strong style={{ fontSize: font.size.base }}>No se pudieron cargar las notificaciones.</strong>
                <span style={{ fontSize: font.size.sm, color: colors.textoTenue }}>{error instanceof Error ? error.message : 'Intenta de nuevo.'}</span>
                <Boton variante="contorno" onClick={() => void refetch()}>Reintentar</Boton>
              </div>
            ) : notificaciones.length === 0 ? (
              <SinNotificaciones rol={u.rol} />
            ) : (
              <ul aria-label="Lista de notificaciones" style={{ listStyle: 'none', margin: 0, padding: 6, display: 'flex', flexDirection: 'column', gap: 4 }}>
                {notificaciones.map((n, i) => <ItemNotificacion key={n.id} n={n} indice={i} onAbrir={() => abrir(n)} />)}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

function ItemNotificacion({ n, indice, onAbrir }: { n: Notificacion; indice: number; onAbrir: () => void }) {
  const t = ESTILO_TIPO[n.tipo];
  return (
    <li className="au-campana-entrada" style={{ ['--i' as string]: Math.min(indice, 8) } as CSSProperties}>
      <button
        type="button" onClick={onAbrir} className="au-campana-item" data-leida={n.leida}
        aria-label={`${n.leida ? '' : 'Sin leer. '}${t.etiqueta}. ${n.titulo}. ${n.mensaje} ${haceCuanto(n.fecha)}`}
        style={{ display: 'flex', alignItems: 'flex-start', gap: 12, width: '100%', textAlign: 'left', padding: '12px 10px', border: 0, borderRadius: radius.lg, background: n.leida ? 'transparent' : colors.marcaSuave, color: colors.texto }}
      >
        <span aria-hidden="true" style={{ width: 36, height: 36, flex: 'none', borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', color: t.color, background: `color-mix(in srgb, ${t.color} 14%, ${colors.superficieFuerte})` }}>
          <Icono d={t.d} tam={18} />
        </span>
        <span style={{ display: 'flex', flexDirection: 'column', gap: 3, minWidth: 0, flex: 1 }}>
          <span style={{ fontSize: font.size.base, fontWeight: n.leida ? font.weight.medium : font.weight.bold, lineHeight: 1.3 }}>{n.titulo}</span>
          <span className="au-campana-mensaje" style={{ fontSize: font.size.sm, color: colors.textoMedio, lineHeight: 1.4 }}>{n.mensaje}</span>
          <span title={fechaCompleta(n.fecha)} style={{ fontSize: font.size.xs, color: colors.textoTenue, marginTop: 1 }}>{haceCuanto(n.fecha)}</span>
        </span>
        {!n.leida && <span aria-hidden="true" style={{ width: 9, height: 9, borderRadius: '50%', background: colors.marca, flex: 'none', marginTop: 6 }} />}
      </button>
    </li>
  );
}

function SinNotificaciones({ rol }: { rol: Rol }) {
  return (
    <div style={{ padding: '36px 24px 40px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 10, textAlign: 'center' }}>
      <span aria-hidden="true" style={{ width: 56, height: 56, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center', background: colors.fondoProfundo, color: colors.textoApagado, marginBottom: 4 }}>
        <Icono d={icono.campana} tam={26} />
      </span>
      <strong style={{ fontSize: font.size.lg }}>No tienes notificaciones</strong>
      <span style={{ fontSize: font.size.sm, color: colors.textoTenue, maxWidth: 280, lineHeight: 1.45 }}>
        {rol === 'estudiante'
          ? 'Te avisaremos cuando tus profesores agreguen una actividad, cambien algo en tus cursos o publiquen una nota.'
          : 'Aquí verás los avisos que lleguen a tu cuenta.'}
      </span>
    </div>
  );
}
